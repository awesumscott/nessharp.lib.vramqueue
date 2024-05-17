using NESSharp.Common;
using NESSharp.Core;
using System;
using static NESSharp.Core.CPU6502;

namespace NESSharp.Lib.VRamQueue.Ops;

public class CallSub {
	private U8 _opCallSub;
	private LiveQueue _liveQueue;
	private Label _executeLoopContinue;
	public CallSub(Func<Label, U8> handlerListAdd, LiveQueue queue, Label execContinue, Label _) {
		_liveQueue = queue;
		_executeLoopContinue = execContinue;
		_opCallSub = handlerListAdd(AL.LabelFor(Handler));
	}
	public void Call(Action action) {
		var offsetAddr = AL.LabelFor(action).Offset(-1);
		_liveQueue.Write(Y, () => {
			_liveQueue.Push(_opCallSub);
			_liveQueue.Push(offsetAddr.Lo());
			_liveQueue.Push(offsetAddr.Hi());
		});
	}
	[CodeSection]
	private void Handler() {
		_liveQueue.Unsafe_Pop(Y);

		AL.TempPtr0.Lo.Set(_liveQueue.Unsafe_Peek(Y));
		_liveQueue.Unsafe_Pop(Y);
		AL.TempPtr0.Hi.Set(_liveQueue.Unsafe_Peek(Y));
		AL.GoSub(AL.LabelFor(StackCall));

		AL.GoTo(_executeLoopContinue);
	}

	[Subroutine]
	private void StackCall() {
		Stack.Push(AL.TempPtr0.Hi);
		Stack.Push(AL.TempPtr0.Lo);
	}
}
