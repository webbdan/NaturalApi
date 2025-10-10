# Common Integration Test Infrastructure

This folder contains shared infrastructure components used by both Simple and Complex integration tests.

## Files

- **`WireMockServers.cs`** - Manages WireMock.NET servers for mocking authentication and API endpoints
- **`CustomApi.cs`** - Custom API implementation that properly configures HttpClient for authentication
- **`AuthenticatedApiDefaults.cs`** - Custom API defaults provider that includes authentication support

## Purpose

These components provide the foundation for integration testing by:
- Setting up mock HTTP servers for authentication and API endpoints
- Providing properly configured API instances with authentication support
- Offering reusable infrastructure that both simple and complex test approaches can use

## Usage

Both Simple and Complex integration tests import these components to avoid duplication and ensure consistent test infrastructure.
