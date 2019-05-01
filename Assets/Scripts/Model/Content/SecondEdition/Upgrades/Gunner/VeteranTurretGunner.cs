﻿using Upgrade;
using Ship;
using Arcs;
using System.Linq;
using ActionsList;
using Actions;
using BoardTools;

namespace UpgradesList.SecondEdition
{
    public class VeteranTurretGunner : GenericUpgrade
    {
        public VeteranTurretGunner() : base()
        {
            UpgradeInfo = new UpgradeCardInfo(
                "Veteran Turret Gunner",
                UpgradeType.Gunner,
                cost: 6,
                abilityType: typeof(Abilities.SecondEdition.VeteranTurretGunnerAbility),
                restriction: new ActionBarRestriction(typeof(RotateArcAction)),
                seImageNumber: 52
            );
        }
    }
}

namespace Abilities.SecondEdition
{
    public class VeteranTurretGunnerAbility : GenericAbility
    {
        // After you perform a primary attack, you may perform a bonus turret arc
        // attack using a turret arc you did not already attack from this round.

        public override void ActivateAbility()
        {
            HostShip.OnAttackFinishAsAttacker += CheckAbility;
            HostShip.Ai.OnGetWeaponPriority += ModifyWeaponPriority;
        }

        public override void DeactivateAbility()
        {
            HostShip.OnAttackFinishAsAttacker -= CheckAbility;
            HostShip.Ai.OnGetWeaponPriority += ModifyWeaponPriority;
        }

        private void CheckAbility(GenericShip ship)
        {
            if (Combat.ShotInfo.Weapon.WeaponType != WeaponTypes.PrimaryWeapon) return;

            bool availableArcsArePresent = HostShip.ArcsInfo.Arcs.Any(a => a.ArcType == ArcType.SingleTurret && !a.WasUsedForAttackThisRound);
            if (availableArcsArePresent)
            {
                HostShip.OnCombatCheckExtraAttack += RegisterSecondAttackTrigger;
            }
            else
            {
                Messages.ShowError(HostUpgrade.UpgradeInfo.Name + " does not have any valid arcs to use.");
            }
        }

        private void RegisterSecondAttackTrigger(GenericShip ship)
        {
            HostShip.OnCombatCheckExtraAttack -= RegisterSecondAttackTrigger;

            RegisterAbilityTrigger(TriggerTypes.OnCombatCheckExtraAttack, UseGunnerAbility);
        }

        private void UseGunnerAbility(object sender, System.EventArgs e)
        {
            if (!HostShip.IsCannotAttackSecondTime)
            {
                HostShip.IsCannotAttackSecondTime = true;

                Combat.StartSelectAttackTarget(
                    HostShip,
                    FinishAdditionalAttack,
                    IsUnusedTurretArcShot,
                    HostUpgrade.UpgradeInfo.Name,
                    "You may perform a bonus turret arc attack using another turret arc",
                    HostUpgrade
                );
            }
            else
            {
                Messages.ShowErrorToHuman(string.Format("{0} cannot attack an additional time", HostShip.PilotInfo.PilotName));
                Triggers.FinishTrigger();
            }
        }

        private void FinishAdditionalAttack()
        {
            // If attack is skipped, set this flag, otherwise regular attack can be performed second time
            HostShip.IsAttackPerformed = true;

            Triggers.FinishTrigger();
        }

        private bool IsUnusedTurretArcShot(GenericShip defender, IShipWeapon weapon, bool isSilent)
        {
            ShotInfo shotInfo = new ShotInfo(HostShip, defender, weapon);
            if (!shotInfo.ShotAvailableFromArcs.Any(a => a.ArcType == ArcType.SingleTurret && !a.WasUsedForAttackThisRound))
            {
                if (!isSilent) Messages.ShowError("Your attack must use a turret arc you have not already attacked from this round.");
                return false;
            }

            if (!weapon.WeaponInfo.ArcRestrictions.Contains(ArcType.SingleTurret))
            {
                if (!isSilent) Messages.ShowError("Your attack must use a turret arc.");
                return false;
            }

            return true;
        }

        private void ModifyWeaponPriority(GenericShip targetShip, IShipWeapon weapon, ref int priority)
        {
            //If this is first attack, and ship can trigger VTG - priorize primary weapon
            if
            (
                !HostShip.IsAttackPerformed
                && weapon.WeaponType == WeaponTypes.PrimaryWeapon
                && CanAttackTargetWithPrimaryWeapon(targetShip)
                && CanAttackTargetWithTurret(targetShip)
            )
            {
                priority += 2000;
            }
        }

        private bool CanAttackTargetWithTurret(GenericShip targetShip)
        {
            foreach (GenericUpgrade turretUpgrade in Selection.ThisShip.UpgradeBar.GetSpecialWeaponsAll())
            {
                IShipWeapon turretWeapon = turretUpgrade as IShipWeapon;
                if (turretWeapon.WeaponType == WeaponTypes.Turret)
                {
                    ShotInfo turretShot = new ShotInfo(HostShip, targetShip, turretWeapon);
                    if (turretShot.IsShotAvailable)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CanAttackTargetWithPrimaryWeapon(GenericShip targetShip)
        {
            ShotInfo primaryShot = new ShotInfo(HostShip, targetShip, HostShip.PrimaryWeapons.First());
            return primaryShot.IsShotAvailable;
        }
    }
}