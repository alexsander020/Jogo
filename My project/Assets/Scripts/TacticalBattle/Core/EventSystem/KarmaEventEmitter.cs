using System;
using System.Collections.Generic;

namespace TacticalBattle.Core.EventSystem
{
    public class KarmaEventEmitter
    {
        private static KarmaEventEmitter _instance;
        public static KarmaEventEmitter Instance => _instance ??= new KarmaEventEmitter();

        public event Action<KarmaEvent> OnKarmaEventDispatched;

        public void Emit(KarmaEvent evt)
        {
            OnKarmaEventDispatched?.Invoke(evt);
        }

        public void EmitTalkChoiceMade(TacticalAttribute alignment, float weight = 5f)
        {
            Emit(new KarmaEvent
            {
                type = KarmaEventType.TalkChoiceMade,
                attribute = alignment,
                weight = weight,
                wasDecisive = false
            });
        }

        public void EmitUnitRecruited(TacticalAttribute unitAttribute, float weight = 10f)
        {
            Emit(new KarmaEvent
            {
                type = KarmaEventType.UnitRecruited,
                attribute = unitAttribute,
                weight = weight,
                wasDecisive = true
            });
        }

        public void EmitCombatAction(TacticalAttribute actionAttribute, bool wasDecisive, float weight = 2f)
        {
            Emit(new KarmaEvent
            {
                type = KarmaEventType.CombatAction,
                attribute = actionAttribute,
                weight = weight,
                wasDecisive = wasDecisive
            });
        }

        public void ClearListeners()
        {
            OnKarmaEventDispatched = null;
        }
    }
}
