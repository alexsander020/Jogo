using System;
using TacticalBattle.Core;
using TacticalBattle.Core.EventSystem;

namespace TacticalBattle.Talk
{
    public static class TalkModule
    {
        public static event Action<string, TalkResult> OnTalkSessionResolved;

        // ==========================================
        // 6.2 FLUXO DE TALK COM INIMIGOS (RECRUTAMENTO - 3 PERGUNTAS)
        // ==========================================
        public static TalkSession StartSession(TalkTarget target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            target.questionsAnswered = 0;
            return new TalkSession(target);
        }

        public static TalkSession AnswerQuestion(
            TalkSession session, 
            PersonalityTrait chosenAnswerAlignment, 
            TalkConfig config = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            config ??= BattleRulesConfig.DefaultTalk;

            if (session.phase == TalkPhase.Resolution)
            {
                return session;
            }

            bool isAligned = chosenAnswerAlignment == session.target.personality;
            float affinityDelta = isAligned ? config.positiveAffinityGain : config.negativeAffinityPenalty;

            // Clamping estrito em [0, 100]
            session.target.affinity = Math.Clamp(session.target.affinity + affinityDelta, 0f, 100f);
            session.target.questionsAnswered += 1;
            session.affinityGainedThisSession += affinityDelta;

            // Transição de fase
            session.phase = session.phase switch
            {
                TalkPhase.Question1 => TalkPhase.Question2,
                TalkPhase.Question2 => TalkPhase.Question3,
                TalkPhase.Question3 => TalkPhase.Resolution,
                _ => TalkPhase.Resolution
            };

            // Se atingiu a resolução, computa o resultado
            if (session.phase == TalkPhase.Resolution)
            {
                session.result = ResolveTalkSession(session);
                OnTalkSessionResolved?.Invoke(session.target.unitId, session.result);

                if (session.result == TalkResult.Recruited)
                {
                    KarmaEventEmitter.Instance.EmitUnitRecruited(TacticalAttribute.Data);
                }
            }

            return session;
        }

        public static TalkResult ResolveTalkSession(TalkSession session)
        {
            if (session == null || session.target == null) return TalkResult.Failed;

            if (session.target.affinity >= session.target.affinityThresholdToRecruit)
            {
                return TalkResult.Recruited;
            }

            if (session.target.affinity >= session.target.itemGrantThreshold)
            {
                return TalkResult.ItemGranted;
            }

            return TalkResult.Failed;
        }
    }
}
