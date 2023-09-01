namespace NetBolt;

public enum ServerDisconnectReason : byte
{
	ExpiredToken,
	Forced,
	InvalidToken,
	PartialMessageViolation,
	Requested,
	Shutdown,
	UnexpectedDisconnect
}
