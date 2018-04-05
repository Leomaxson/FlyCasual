﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SubPhases;

namespace MainPhases
{

    public class ActivationPhase : GenericPhase
    {

        public override void StartPhase()
        {
            Name = "Activation Phase";

            Phases.CurrentSubPhase = new ActivationStartSubPhase();
            Phases.CurrentSubPhase.Start();
            Phases.CurrentSubPhase.Prepare();
            Phases.CurrentSubPhase.Initialize();
        }

        public override void NextPhase()
        {
            Selection.DeselectAllShips();

            GenericSubPhase subphase = Phases.StartTemporarySubPhaseNew("Notification", typeof(NotificationSubPhase), StartCombatPhase);
            (subphase as NotificationSubPhase).TextToShow = "Combat";
            subphase.Start();
        }

        private void StartCombatPhase()
        {
            Phases.CurrentPhase = new CombatPhase();
            Phases.CurrentPhase.StartPhase();
        }

    }

}
