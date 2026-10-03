namespace EtcdTerminal.Presentation;

public interface ILiveFrame
{
	T Run<T>(FrameModel initial, LiveFrameEnd end, Func<ILiveFrameUpdater, T> interaction);
}
