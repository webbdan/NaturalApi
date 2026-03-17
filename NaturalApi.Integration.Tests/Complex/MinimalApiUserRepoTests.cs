using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaturalApi;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace NaturalApi.Integration.Tests.Complex;

[TestClass]
public class MinimalApiUserRepoTests
{
    private HttpListener? _listener;
    private int _port;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;

    [TestInitialize]
    public void Setup()
    {
        // pick an available TCP port
        var tl = new TcpListener(IPAddress.Loopback, 0);
        tl.Start();
        _port = ((IPEndPoint)tl.LocalEndpoint).Port;
        tl.Stop();

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
        _listener.Start();

        var repo = new InMemoryUserRepository();
        _cts = new CancellationTokenSource();
        _listenerTask = Task.Run(() => ListenLoop(_listener, repo, _cts.Token));
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener?.Close();
            _listenerTask?.Wait(1000);
        }
        catch { }
    }

    [TestMethod]
    public void MinimalApi_UserRepo_ShouldPersistBetweenRequests()
    {
        // Arrange - create NaturalApi with HttpClient that talks to our in-process listener
        using var httpClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{_port}") };
        var api = new Api(new HttpClientExecutor(httpClient));

        // Act & Assert - initial GET should return empty list
        var list1 = api.For("/users").Get().ShouldReturn<List<User>>();
        Assert.IsNotNull(list1);
        Assert.AreEqual(0, list1.Count);

        // POST a new user
        var toCreate = new UserCreate { name = "Alice", email = "alice@example.com" };
        var postResult = api.For("/users").Post(toCreate);
        Assert.IsTrue(postResult.StatusCode >= 200 && postResult.StatusCode < 300);

        // Try to deserialize the created user
        var created = postResult.ShouldReturn<User>();
        Assert.IsNotNull(created);
        Assert.AreEqual("Alice", created.name);
        Assert.IsTrue(created.id > 0);

        // GET again - should contain the newly added user
        var list2 = api.For("/users").Get().ShouldReturn<List<User>>();
        Assert.IsNotNull(list2);
        Assert.AreEqual(1, list2.Count);
        Assert.AreEqual("Alice", list2[0].name);
    }

    private async Task ListenLoop(HttpListener listener, InMemoryUserRepository repo, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var ctxTask = listener.GetContextAsync();
                var completed = await Task.WhenAny(ctxTask, Task.Delay(-1, token));
                if (completed != ctxTask) break;
                var ctx = ctxTask.Result;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var req = ctx.Request;
                        var resp = ctx.Response;
                        resp.ContentType = "application/json";

                        if (req.HttpMethod == "GET" && req.Url?.AbsolutePath == "/users")
                        {
                            var users = repo.GetAll();
                            var json = JsonSerializer.Serialize(users);
                            var bytes = Encoding.UTF8.GetBytes(json);
                            resp.StatusCode = 200;
                            resp.ContentLength64 = bytes.Length;
                            await resp.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                        }
                        else if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/users")
                        {
                            using var sr = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
                            var body = await sr.ReadToEndAsync();
                            try
                            {
                                var created = JsonSerializer.Deserialize<UserCreate>(body) ?? new UserCreate();
                                var user = repo.Create(new User { name = created.name, email = created.email });
                                var json = JsonSerializer.Serialize(user);
                                var bytes = Encoding.UTF8.GetBytes(json);
                                resp.StatusCode = 201;
                                resp.ContentLength64 = bytes.Length;
                                await resp.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                            }
                            catch (JsonException)
                            {
                                resp.StatusCode = 400;
                            }
                        }
                        else
                        {
                            resp.StatusCode = 404;
                        }

                        resp.OutputStream.Close();
                    }
                    catch
                    {
                        // ignore per-request errors
                    }
                }, token);
            }
        }
        catch (OperationCanceledException) { }
        catch { }
    }

    // Minimal models and in-memory repository used only for this test
    private record UserCreate
    {
        public string name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
    }

    public class User
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
    }

    public interface IUserRepository
    {
        List<User> GetAll();
        User Create(User user);
    }

    public class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> _users = new();
        private int _next = 1;
        private readonly object _lock = new();

        public List<User> GetAll()
        {
            lock (_lock)
            {
                // return copies to avoid external mutation
                return _users.Select(u => new User { id = u.id, name = u.name, email = u.email }).ToList();
            }
        }

        public User Create(User user)
        {
            lock (_lock)
            {
                user.id = _next++;
                // store a copy
                var stored = new User { id = user.id, name = user.name, email = user.email };
                _users.Add(stored);
                return new User { id = stored.id, name = stored.name, email = stored.email };
            }
        }
    }
}
