using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using CombatExtended;
using Verse.Sound;

namespace Jams_nmags
{
	

	public class Verb_ShootWithReliability : Verb_LaunchProjectileCE
	{

		protected override int ShotsPerBurst
		{
			get
			{
				if (base.CompFireModes != null)
				{
					if (base.CompFireModes.CurrentFireMode == FireMode.SingleFire)
					{
						return 1;
					}
					if (base.CompFireModes.CurrentFireMode == FireMode.BurstFire && base.CompFireModes.Props.aimedBurstShotCount > 0)
					{
						return base.CompFireModes.Props.aimedBurstShotCount;
					}
				}
				return base.VerbPropsCE.burstShotCount;
			}
		}


		private bool ShouldAim
		{
			get
			{
				if (base.CompFireModes != null)
				{
					if (base.ShooterPawn != null)
					{
						if (base.ShooterPawn.CurJob != null && base.ShooterPawn.CurJob.def == JobDefOf.Hunt)
						{
							return true;
						}
						if (this.IsSuppressed)
						{
							return false;
						}
						if (base.ShooterPawn.pather.Moving)
						{
							return false;
						}
					}
					return base.CompFireModes.CurrentAimMode == AimMode.AimedShot;
				}
				return false;
			}
		}

		public ReliabilityComp RelComp
		{
			get
			{
				return this.EquipmentSource.TryGetComp<ReliabilityComp>();
			}
		}


		protected override float SwayAmplitude
		{
			get
			{
				float num = base.SwayAmplitude;
				if (this.ShouldAim)
				{
					num = num / Mathf.Max(1f, base.EquipmentSource.GetStatValue(CE_StatDefOf.SightsEfficiency, true)) * Mathf.Max(0f, 1f - base.AimingAccuracy);
				}
				else if (this.IsSuppressed)
				{
					num *= 1.5f;
				}
				return num;
			}
		}


		private bool IsSuppressed
		{
			get
			{
				Pawn shooterPawn = base.ShooterPawn;
				bool? flag;
				if (shooterPawn == null)
				{
					flag = null;
				}
				else
				{
					CompSuppressable compSuppressable = shooterPawn.TryGetComp<CompSuppressable>();
					flag = ((compSuppressable != null) ? new bool?(compSuppressable.isSuppressed) : null);
				}
				return flag ?? false;
			}
		}


		public override void WarmupComplete()
		{
			float lengthHorizontal = (this.currentTarget.Cell - this.caster.Position).LengthHorizontal;
			int num = (int)Mathf.Lerp(30f, 240f, lengthHorizontal / 100f);
			if (this.ShouldAim && !this._isAiming)
			{
				Building_TurretGunCE building_TurretGunCE;
				if ((building_TurretGunCE = (this.caster as Building_TurretGunCE)) != null)
				{
					building_TurretGunCE.burstWarmupTicksLeft += num;
					this._isAiming = true;
					return;
				}
				if (base.ShooterPawn != null)
				{

					base.ShooterPawn.stances.SetStance(new Stance_Warmup(num, this.currentTarget, this));
					this._isAiming = true;

					return;
				}
			}
			base.WarmupComplete();
			this._isAiming = false;
			Pawn shooterPawn = base.ShooterPawn;
			if (((shooterPawn != null) ? shooterPawn.skills : null) != null && this.currentTarget.Thing is Pawn)
			{
				float num2 = this.verbProps.AdjustedFullCycleTime(this, base.ShooterPawn);
				num2 += num.TicksToSeconds();
				float num3 = this.currentTarget.Thing.HostileTo(base.ShooterPawn) ? 170f : 20f;
				num3 *= num2;
				base.ShooterPawn.skills.Learn(SkillDefOf.Shooting, num3, false);
			}
		}


		public override void VerbTickCE()
		{
			if (this._isAiming)
			{
				if (!this.ShouldAim)
				{
					this.WarmupComplete();
				}
				if (!(this.caster is Building_TurretGunCE))
				{
					Pawn shooterPawn = base.ShooterPawn;
					Type left;
					if (shooterPawn == null)
					{
						left = null;
					}
					else
					{
						Stance curStance = shooterPawn.stances.curStance;
						left = ((curStance != null) ? curStance.GetType() : null);
					}
					if (left != typeof(Stance_Warmup))
					{
						this._isAiming = false;
					}
				}
			}
		}


		public override void Notify_EquipmentLost()
		{
			base.Notify_EquipmentLost();
			if (base.CompFireModes != null)
			{
				base.CompFireModes.ResetModes();
			}
		}


		public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
		{
			return (base.ShooterPawn == null || base.ShooterPawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight)) && base.CanHitTargetFrom(root, targ);
		}
		public Job TryMakeReloadJob()
		{
			return new Job(TymonsJobDefOf.ClearMalfunction, EquipmentSource);
		}

		protected override bool TryCastShot()
		{
			if (RelComp.Jammed == true)
			{
				
					if (TymonsJobDefOf.ClearMalfunction != null)
					{
						
						//CasterPawn.jobs.StartJob(JobMaker.MakeJob(TymonsJobDefOf.ClearMalfunction));
						//Job blobl = Job(TymonsJobDefOf.ClearMalfunction, EquipmentSource);
						ThinkNode jobGiver = null;
						Pawn_JobTracker jobs = this.CasterPawn.jobs;
						Job job = this.TryMakeReloadJob();
						Job newJob = job;
						JobCondition lastJobEndCondition = JobCondition.InterruptForced;
						Job curJob = this.CasterPawn.CurJob;
						jobs.StartJob(newJob, lastJobEndCondition, jobGiver, ((curJob != null) ? curJob.def : null) != job.def, true, null, null, false, false);
					}
					else
					{
						Log.Error("JobDef of is nool");
					}
					
				
				return false;
			}
			if (base.CompAmmo != null && !base.CompAmmo.TryReduceAmmoCount(base.VerbPropsCE.ammoConsumedPerShotCount))
			{
				return false;
			}
			if (!base.TryCastShot())
			{
				return false;
			}
			if (base.ShooterPawn != null)
			{
				base.ShooterPawn.records.Increment(RecordDefOf.ShotsFired);
			}

			if (base.CompAmmo != null && !base.CompAmmo.HasMagazine && base.CompAmmo.UseAmmo)
			{
				if (!base.CompAmmo.Notify_ShotFired())
				{
					if (base.VerbPropsCE.muzzleFlashScale > 0.01f)
					{
						MoteMaker.MakeStaticMote(this.caster.Position, this.caster.Map, ThingDefOf.Mote_ShotFlash, base.VerbPropsCE.muzzleFlashScale);
					}
					if (base.VerbPropsCE.soundCast != null)
					{
						base.VerbPropsCE.soundCast.PlayOneShot(new TargetInfo(this.caster.Position, this.caster.Map, false));
					}
					if (base.VerbPropsCE.soundCastTail != null)
					{
						base.VerbPropsCE.soundCastTail.PlayOneShotOnCamera(null);
					}
					if (base.ShooterPawn != null && base.ShooterPawn.thinker != null)
					{
						base.ShooterPawn.mindState.lastEngageTargetTick = Find.TickManager.TicksGame;
					}
					
				}
				RelComp.RunJamTest();
				return base.CompAmmo.Notify_PostShotFired();
			}

			RelComp.RunJamTest();
			//Log.Error(RelComp.Jammed.ToString());
			return true;
		}


		private const int AimTicksMin = 30;


		private const int AimTicksMax = 240;


		private const float PawnXp = 20f;


		private const float HostileXp = 170f;


		private const float SuppressionSwayFactor = 1.5f;


		private bool _isAiming;
	}
	public class ReliabilityComp : ThingComp
	{
		public int Reliability => Props.Reliability;
		public bool Jammed = false;
		public float Qualitymod = 1f;
		
		

		

		
		public ReliabilityCompProps Props => (ReliabilityCompProps)this.props;
		public CompQuality QualComp
		{
			get
			{
				return this.parent.TryGetComp<CompQuality>();
			}
		}
		
		public bool RunJamTest()
		{
			if (QualComp.Quality == QualityCategory.Awful)
			{
				Qualitymod = 0.65f;
			}
			if (QualComp.Quality == QualityCategory.Poor)
			{
				Qualitymod = 0.75f;
			}
			if (QualComp.Quality == QualityCategory.Normal)
			{
				Qualitymod = 1.025f;
			}
			if (QualComp.Quality == QualityCategory.Good)
			{
				Qualitymod = 1.05f;
			}
			if (QualComp.Quality == QualityCategory.Excellent)
			{
				Qualitymod = 1.1f;
			}
			if (QualComp.Quality == QualityCategory.Masterwork)
			{
				Qualitymod = 1.2f;
			}
			if (QualComp.Quality == QualityCategory.Legendary)
			{
				Qualitymod = 1.5f;
			}
			float checker = this.parent.MaxHitPoints;
			float checker2 = this.parent.HitPoints;
			float checker3 = checker2 / checker;
			float check = Reliability * Qualitymod * checker3;
			if (check < Rand.Range(0, 10000))
			{
				Log.Error(check.ToString());
				Jammed = true;
				if (check < 2000)
				{
					float obama = Rand.Range(1, 3);
					
					if (5000 < Rand.Range(0, 10000))
					{
						GenExplosionCE.DoExplosion(this.parent.PositionHeld, this.parent.MapHeld, 5, DamageDefOf.Bomb, null, Rand.Range(2, 10), obama, SoundDefOf.Click, null, null, null, null, 0, 0, false, null, 0f, 0, 0.5f, false, null, null, 0f, 1, true, this.parent);
						return false;

					}
					

				}
				return true;
			}
			else
			{

				return false;
			}
			
			
		}
		public void Clear()
		{
			Jammed = false;
		}

	}

	public class ReliabilityCompProps : CompProperties
	{
		public int Reliability;

		public ReliabilityCompProps()
		{
			this.compClass = typeof(ReliabilityComp);
		}

		public ReliabilityCompProps(Type compClass) : base(compClass)
		{
			this.compClass = compClass;
		}
	
	}
	public class ClearJam : JobDriver
	{
		private ThingWithComps weapon
		{
			get
			{
				return base.TargetThingA as ThingWithComps;
			}
		}

		private const TargetIndex GunToUnjam = TargetIndex.A;
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			//return true;	
			return this.pawn.Reserve(this.job.GetTarget(GunToUnjam), this.job, 1, -1, null);
		}
		public ReliabilityComp ReliabilityComp
		{
			get
			{
				return this.weapon.TryGetComp<ReliabilityComp>();
			}
		}
		protected override IEnumerable<Toil> MakeNewToils()
		{
			Toil toil = Toils_General.Wait(Rand.Range(240, 720) / (this.pawn.skills.GetSkill(SkillDefOf.Shooting).Level / 2));

			toil.AddFinishAction(delegate
			{
				this.ReliabilityComp.Clear();
				this.ReliabilityComp.Jammed = false;
			});
			yield return toil;
			
			//Log.Error("yes");
		}
		

	}


}
		