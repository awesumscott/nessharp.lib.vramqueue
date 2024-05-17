using NESSharp.Common;
using NESSharp.Core;
using System;
using static NESSharp.Core.CPU6502;

namespace NESSharp.Lib.VRamQueue.Ops;

public class Tile {
	private U8 _opTile;
	private LiveQueue _liveQueue;
	private Label _executeLoopContinue;
	public Tile(Func<Label, U8> handlerListAdd, LiveQueue queue, Label execContinue, Label _) {
		_liveQueue = queue;
		_executeLoopContinue = execContinue;
		_opTile = handlerListAdd(AL.LabelFor(Handler));
	}
	public void DrawTile(IOperand tile) {
		_liveQueue.Write(Y, () => {
			_liveQueue.Push(_opTile);
			_liveQueue.Push(tile);
		});
	}
	public void DrawTile(Func<IOperand> tile) {
		_liveQueue.Write(Y, () => {
			_liveQueue.Push(_opTile);
			_liveQueue.Push(tile());
		});
	}
	[CodeSection]
	private void Handler() {
		_liveQueue.Unsafe_Pop(Y);
		NES.PPU.Data.Set(_liveQueue.Unsafe_Peek(Y));
		AL.GoTo(_executeLoopContinue);
	}
}
