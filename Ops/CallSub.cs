using NESSharp.Common;
using NESSharp.Core;
using System;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.Ops {
	public class CallSub {
		private U8 _opCallSub;
		private LiveQueue _liveQueue;
		private Label _executeLoopContinue;
		public CallSub(Func<Label, U8> handlerListAdd, LiveQueue queue, Label execContinue, Label _) {
			_liveQueue = queue;
			_executeLoopContinue = execContinue;
			_opCallSub = handlerListAdd(LabelFor(Handler));
		}
		public void Call(Action action) {
			var offsetAddr = LabelFor(action).Offset(-1);
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opCallSub);
				_liveQueue.Push(offsetAddr.Lo());
				_liveQueue.Push(offsetAddr.Hi());
			});
		}
		[CodeSection]
		private void Handler() {
			_liveQueue.Unsafe_Pop(Y);
			
			TempPtr0.Lo.Set(_liveQueue.Unsafe_Peek(Y));
			_liveQueue.Unsafe_Pop(Y);
			TempPtr0.Hi.Set(_liveQueue.Unsafe_Peek(Y));
			GoSub(LabelFor(StackCall));

			GoTo(_executeLoopContinue);
		}

		[Subroutine]
		private void StackCall() {
			Stack.Push(TempPtr0.Hi);
			Stack.Push(TempPtr0.Lo);
		}
	}
}
