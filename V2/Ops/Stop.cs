using NESSharp.Core;

namespace NESSharp.Lib.VRamQueue.V2.Ops;

public class Stop : VRamQueueOp {
	public override void AddHandlers() => Queue.Add(Handler);

	[CodeSection]
	public void Handler() {
		//Skip incrementing Y, so next read takes place at the same index, and rely on Stop being overwritten
		AL.Comment("Stop updates until more data is queued");
		Queue.Done();
		Queue.Break();
	}
}
