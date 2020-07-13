using System;
using System.Collections.Generic;
using System.Text;
using NESSharp.Common;
using NESSharp.Core;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.V2.Ops {
	public class FromAddress : VRamQueueOp {
		private LiveQueue _liveQueue;
		public FromAddress(LiveQueue queue) {
			_liveQueue = queue;
		}
		public override void AddHandlers() => Queue.Add(Handler);
		public void Write(Core.Address ramStart, U8 len) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(Id);
				_liveQueue.Push(ramStart.Lo);
				_liveQueue.Push(ramStart.Hi);
				_liveQueue.Push(len);
			});
		}
		public void Write(Label lbl, U8 len) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(Id);
				_liveQueue.Push(lbl.Lo());
				_liveQueue.Push(lbl.Hi());
				_liveQueue.Push(len);
			});
		}
		public void WriteROM(Label lbl, U8 len) {
			Raw(Id);
			Raw(lbl.Lo());
			Raw(lbl.Hi());
			Raw(len);
		}
		[CodeSection]
		private void Handler() {
			_liveQueue.Unsafe_Pop(Y);
			TempPtr0.Lo.Set(_liveQueue.Unsafe_Peek(Y));
			_liveQueue.Unsafe_Pop(Y);
			TempPtr0.Hi.Set(_liveQueue.Unsafe_Peek(Y));
			_liveQueue.Unsafe_Pop(Y);
			X.Set(_liveQueue.Unsafe_Peek(Y)); //Number of bytes of data

			Stack.Preserve(Y, () => {
				Y.Set(0);
				Loop.Descend(X, _ => {
					NES.PPU.Data.Set(TempPtr0[Y]);
					Y++;
				});
			});

			Queue.Continue();
		}
	}
}
