﻿using NESSharp.Common;
using NESSharp.Core;
using System;
using static NESSharp.Core.CPU6502;

namespace NESSharp.Lib.VRamQueue.V2.Ops;

public class TilesRLE : VRamQueueOp {
	private U8 _opTilesRLE;
	private LiveQueue _liveQueue;
	private Label _executeLoopContinue;
	public override void AddHandlers() => Queue.Add(Handler);
	public TilesRLE(Func<Label, U8> handlerListAdd, LiveQueue queue, Label execContinue, Label _) {
		_liveQueue = queue;
		_executeLoopContinue = execContinue;
		_opTilesRLE = handlerListAdd(AL.LabelFor(Handler));
	}
	public void Draw(params U8[] tile) {
		_liveQueue.Write(Y, () => {
			_liveQueue.Push(_opTilesRLE);
			for (var i = 0; i < tile.Length; i++) { //using tile.length here so individual values can be pushed after this call
				_liveQueue.Push(tile[i]);
			}
			_liveQueue.Push(Queue.Op<NOP>().Id);
		});
	}
	public void Draw_Manual() {
		_liveQueue.PushStart(Y);
		_liveQueue.Push(_opTilesRLE);
	}
	public void Draw_Manual_Done() => _liveQueue.Push(Queue.Op<NOP>().Id);
	[CodeSection]
	private void Handler() {
		_liveQueue.Unsafe_Pop(Y);
		X.Set(_liveQueue.Unsafe_Peek(Y)); //Number of bytes of data
		Loop.While_PreCondition_NoInc(() => X.NotEquals(Queue.Op<NOP>().Id), _ => {
			_liveQueue.Unsafe_Pop(Y);
			A.Set(_liveQueue.Unsafe_Peek(Y));
			Loop.Descend_PostCondition_PostDec(X, _ => {
				NES.PPU.Data.Set(A);
			});
			_liveQueue.Unsafe_Pop(Y);
			X.Set(_liveQueue.Unsafe_Peek(Y));
		});
		AL.GoTo(_executeLoopContinue);
	}
}
