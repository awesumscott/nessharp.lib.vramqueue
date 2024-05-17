using NESSharp.Core;

namespace NESSharp.Lib.VRamQueue.V2.Ops;

public class EOF : VRamQueueOp {
	public override void AddHandlers() => Queue.Add(Handler);
	[CodeSection]
	private void Handler() {
		AL.Comment("End of frame");
		Queue.Unsafe_Pop();
		Queue.Break();
	}
}
