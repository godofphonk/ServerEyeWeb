# Go API Integration Documentation

## Overview

This document describes the integration between ServerEye Web backend and Go API for server source and identifiers management.

## Architecture

When a user adds a server to their account, the system now performs the following operations:

1. **Validate Server Key** - Validates the server key with Go API
2. **Add Web Source** - Registers "Web" as a source for the server in Go API
3. **Add User Identifiers** - Associates the user ID with the Web source in Go API
4. **Database Operations** - Continues with existing database operations

## New Go API Endpoints

### 1. Add Server Source

**Endpoint:** `POST /api/servers/{server_id}/sources`  
**Alternative:** `POST /api/servers/by-key/{server_key}/sources`

**Request Body:**
```json
{
  "source": "Web"
}
```

**Response:**
```json
{
  "server_id": "srv_123456789",
  "source": "Web",
  "message": "Source added successfully"
}
```

### 2. Add Source Identifiers

**Endpoint:** `POST /api/servers/{server_id}/sources/identifiers`  
**Alternative:** `POST /api/servers/by-key/{server_key}/sources/identifiers`

**Request Body:**
```json
{
  "source_type": "Web",
  "identifiers": ["user123", "user456"],
  "identifier_type": "user_id",
  "metadata": {
    "session_info": "web_session_123",
    "user_agent": "Mozilla/5.0...",
    "added_at": "2026-03-08T18:58:00Z",
    "source": "ServerEyeWeb"
  }
}
```

**Response:**
```json
{
  "message": "Identifiers added successfully",
  "server_id": "srv_123456789",
  "source_type": "Web",
  "identifiers": ["user123", "user456"],
  "identifier_type": "user_id"
}
```

## Implementation Details

### New DTO Classes

#### GoApiSourceRequest.cs
```csharp
public class GoApiSourceRequest
{
    public string Source { get; set; } = string.Empty;
}

public class GoApiSourceResponse
{
    public string ServerId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
```

#### GoApiSourceIdentifiersRequest.cs
```csharp
public class GoApiSourceIdentifiersRequest
{
    public string SourceType { get; set; } = string.Empty;
    public List<string> Identifiers { get; set; } = new();
    public string IdentifierType { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

public class GoApiSourceIdentifiersResponse
{
    public string Message { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public List<string> Identifiers { get; set; } = new();
    public string IdentifierType { get; set; } = string.Empty;
}
```

### Updated Interfaces

#### IGoApiClient
Added new methods:
- `AddServerSourceAsync(string serverId, string source)`
- `AddServerSourceByKeyAsync(string serverKey, string source)`
- `AddServerSourceIdentifiersAsync(string serverId, GoApiSourceIdentifiersRequest request)`
- `AddServerSourceIdentifiersByKeyAsync(string serverKey, GoApiSourceIdentifiersRequest request)`

### Service Integration

#### ServerAccessService.AddServerAsync()
Modified to:
1. Call Go API to add "Web" source
2. Call Go API to add user identifier with metadata
3. Continue with existing database operations
4. Log warnings if Go API calls fail but don't prevent server addition

## Error Handling

The integration follows a graceful degradation approach:
- If Go API source addition fails, the operation continues with a warning
- If Go API identifiers addition fails, the operation continues with a warning
- Database operations are not dependent on Go API success
- All failures are logged for monitoring

## Metadata

When adding user identifiers, the system includes the following metadata:
- `added_at`: Timestamp when the user was added
- `source`: Always set to "ServerEyeWeb"

## Testing

### Unit Tests Created

1. **GoApiClientTests.cs**
   - Tests all new Go API client methods
   - Tests success and failure scenarios
   - Verifies correct HTTP requests are made

2. **ServerAccessServiceTests.cs**
   - Tests integration with Go API in AddServerAsync
   - Tests both new server and existing server scenarios
   - Tests graceful degradation when Go API fails
   - Tests that operations continue when Go API is unavailable

### Test Coverage

- ✅ Successful Go API integration
- ✅ Go API failure scenarios
- ✅ HTTP request verification
- ✅ Request/response serialization
- ✅ Error handling and logging
- ✅ Database operation independence

## Configuration

No additional configuration is required. The Go API client uses the existing HttpClient configuration and logging setup.

## Monitoring

The following events are logged:
- Go API source addition success/failure
- Go API identifiers addition success/failure
- Server addition completion
- Any errors during the process

## Future Enhancements

Potential improvements:
1. Retry logic for failed Go API calls
2. Circuit breaker pattern for Go API resilience
3. Metrics collection for Go API performance
4. Background job to sync existing servers with Go API
5. Support for additional source types (TGBot, Email)
