using System;
using TacticalBattle.Core;
using TacticalBattle.Core.EventSystem;

namespace TacticalBattle.Attribute
{
    public class KarmaModule
    {
        public KarmaState Karma { get; private set; }

        public KarmaModule(KarmaState initialKarma = null)
        {
            Karma = initialKarma ?? new KarmaState();
            KarmaEventEmitter.Instance.OnKarmaEventDispatched += ProcessKarmaEvent;
        }

        public void Dispose()
        {
            KarmaEventEmitter.Instance.OnKarmaEventDispatched -= ProcessKarmaEvent;
        }

        public void ProcessKarmaEvent(KarmaEvent evt)
        {
            switch (evt.attribute)
            {
                case TacticalAttribute.Vaccine:
                    Karma.AddMoral(evt.weight);
                    break;
                case TacticalAttribute.Data:
                    Karma.AddHarmony(evt.weight);
                    break;
                case TacticalAttribute.Virus:
                    Karma.AddWrath(evt.weight);
                    break;
                case TacticalAttribute.Free:
                    // Sem alteração direta no Karma
                    break;
            }
        }

        public TacticalAttribute GetDominantAlignment()
        {
            if (Karma.moral >= Karma.harmony && Karma.moral >= Karma.wrath)
            {
                return TacticalAttribute.Vaccine;
            }
            if (Karma.harmony >= Karma.moral && Karma.harmony >= Karma.wrath)
            {
                return TacticalAttribute.Data;
            }
            return TacticalAttribute.Virus;
        }
    }
}
