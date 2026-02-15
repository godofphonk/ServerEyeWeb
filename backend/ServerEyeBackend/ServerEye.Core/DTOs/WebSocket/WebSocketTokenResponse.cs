namespace ServerEye.Core.DTOs.WebSocket;

public class WebSocketTokenResponse
{
    public string Token { get; set; } = string.Empty;
    public Uri WsUrl { get; set; } = new Uri("ws://localhost:8080/ws");
    public DateTime ExpiresAt { get; set; }
}
