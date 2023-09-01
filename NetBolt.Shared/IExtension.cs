namespace NetBolt.Shared;

public interface IExtension
{
	string Name { get; }

	void Start();
	void Stop();
	void ProcessEvents();
}
