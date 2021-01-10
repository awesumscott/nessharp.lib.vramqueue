using NESSharp.Common;
using NESSharp.Core;
using System;
using System.Collections.Generic;
using System.Text;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.V2.Ops {
	public class Pause : VRamQueueOp {
		private readonly VByte _pauseCount;
		public Pause() {
			_pauseCount = VByte.New(NES.ram, "VRamQueue_pauseCount");
		}
		public override void AddHandlers() => Queue.Add(Handler);
		public void For(U8 frames) {
			Queue._liveQueue.Write(Y, () => {
				Queue._liveQueue.Push(Id);
				Queue._liveQueue.Push(frames);
			});
		}

		[CodeSection]
		private void Handler() {
			Comment("Pause");
			Queue._liveQueue.Unsafe_Pop(Y);
			_pauseCount.Set(Queue._liveQueue.Unsafe_Peek(Y));
			Y++;
			Queue.Break();
		}

		public void ExecuteBlockWrapper(Action block) {
			If.Block(c => c
				.True(() => _pauseCount.Equals(0), block)
				.Else(() => {
					_pauseCount.Decrement();
					Queue.Break();
				})
			);
		}
		
		public void Reset() {
			_pauseCount.Set(0);
		}
	}
}
