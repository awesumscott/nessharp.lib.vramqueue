using NESSharp.Common;
using NESSharp.Core;
using System;
using System.Collections.Generic;
using System.Text;
using static NESSharp.Core.CPU6502;

namespace NESSharp.Lib.VRamQueue.Ops;

public class Increment {
	private U8 _opHoriz;
	private U8 _opVert;
	private LiveQueue _liveQueue;
	private Label _executeLoopContinue;
	public Increment(Func<Label, U8> handlerListAdd, LiveQueue queue, Label execContinue, Label _) {
		_liveQueue = queue;
		_executeLoopContinue = execContinue;
		_opHoriz = handlerListAdd(AL.LabelFor(Handler_Horiz));
		_opVert = handlerListAdd(AL.LabelFor(Handler_Vert));
	}
	public void Horizontal() {
		_liveQueue.PushOnce(Y, _opHoriz);
	}
	public void HorizontalROM() {
		AL.Raw(_opHoriz);
	}
	public void Vertical() {
		_liveQueue.PushOnce(Y, _opVert);
	}
	public void VerticalROM() {
		AL.Raw(_opVert);
	}
	[CodeSection]
	private void Handler_Horiz() {
		AL.Comment("Increment writes horizontally");
		NES.PPU.Control.Set(NES.PPU.LazyControl.Set(z => z.And(0b11111011)));
		AL.GoTo(_executeLoopContinue);
	}

	[CodeSection]
	private void Handler_Vert() {
		AL.Comment("Increment writes vertically");
		NES.PPU.Control.Set(NES.PPU.LazyControl.Set(z => z.Or(0b100)));
		AL.GoTo(_executeLoopContinue);
	}
}
