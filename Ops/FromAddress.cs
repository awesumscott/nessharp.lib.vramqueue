using NESSharp.Common;
using NESSharp.Core;
using System;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.Ops {
	public class FromAddress {
		private U8 _opFromAddress;
		private LiveQueue _liveQueue;
		private Label _executeLoopContinue;
		public FromAddress(Func<Label, U8> handlerListAdd, LiveQueue queue, Label execContinue, Label _) {
			_liveQueue = queue;
			_executeLoopContinue = execContinue;
			_opFromAddress = handlerListAdd(LabelFor(Handler));
		}
		public void Write(Core.Address ramStart, U8 len) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opFromAddress);
				_liveQueue.Push(ramStart.Lo);
				_liveQueue.Push(ramStart.Hi);
				_liveQueue.Push(len);
			});
		}
		public void Write(Core.VWord ramStart, U8 len) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opFromAddress);
				_liveQueue.Push(ramStart.Lo);
				_liveQueue.Push(ramStart.Hi);
				_liveQueue.Push(len);
			});
		}
		public void Write(Label lbl, U8 len) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opFromAddress);
				_liveQueue.Push(lbl.Lo());
				_liveQueue.Push(lbl.Hi());
				_liveQueue.Push(len);
			});
		}
		public void WriteROM(Label lbl, U8 len) {
			Raw(_opFromAddress);
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
				Loop.Descend_Post(X, _ => {
					NES.PPU.Data.Set(TempPtr0[Y]);
					Y++;
				});
			});

			GoTo(_executeLoopContinue);
		}
	}
}
