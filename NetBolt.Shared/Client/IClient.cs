namespace NetBolt;

public interface IClient
{
	double Ping { get; }
	ClientIdentifier Identifier { get; }
}
