using System;
using RevertToStoneAge;
class ModelTests
{
 static int checks;
 static void Check(bool value, string name) { checks++; if(!value) throw new Exception(name); }
 static int Main()
 {
  Check(BalanceRules.IsAboveCoverage(0,1000,0,70),"zenith excluded");
  Check(!BalanceRules.IsAboveCoverage(0,1000,0,90),"90 disables blind zone");
  Check(!BalanceRules.IsAboveCoverage(0,-1000,0,70),"below radar unaffected");
  Check(!BalanceRules.IsAboveCoverage(0,0,0,70),"coincident positions");
  foreach(float range in new[]{10f,1000f,100000f}) foreach(float azimuth in new[]{0f,90f,180f,270f}) {
   double az=azimuth*Math.PI/180;
   foreach(float elevation in new[]{-10f,0f,69.99f,70.01f,89f}) {
    float x=(float)(range*Math.Cos(az)); float z=(float)(range*Math.Sin(az)); float y=(float)(range*Math.Tan(elevation*Math.PI/180));
    Check(BalanceRules.IsAboveCoverage(x,y,z,70)==(elevation>70),"boundary/azimuth/range");
   }
  }
  int[] counts=new int[6];
  for(int i=0;i<10000;i++) counts[(int)BalanceRules.SelectFailure((i+0.5f)/10000)]++;
  int[] expected={1000,1500,2000,2000,1500,2000};
  for(int i=0;i<6;i++) Check(counts[i]==expected[i],"failure distribution "+i);
  foreach(MissileFailure failure in Enum.GetValues(typeof(MissileFailure))) {
   Check(!BalanceRules.IsMotorBlocked(failure,0.9f,1f,1.6f),"not before onset");
   bool permanent=failure==MissileFailure.IgnitionFailure || failure==MissileFailure.MotorCutout;
   Check(BalanceRules.IsMotorBlocked(failure,1.1f,1f,1.6f)==(permanent||failure==MissileFailure.IgnitionDelay),"active behavior");
   Check(BalanceRules.IsMotorBlocked(failure,1.6f,1f,1.6f)==permanent,"delay resumes at exact endpoint");
   Check(BalanceRules.IsMotorBlocked(failure,100f,1f,1.6f)==permanent,"late behavior");
  }
  Check(BalanceRules.ScaleTiming(0, 2.5f, 1.2f)==1.2f,"zero planning receives a floor");
  Check(BalanceRules.ScaleTiming(2, 2.5f, 1.2f)==5f,"native planning scales");
  float stability=0f;
  for(int i=0;i<60;i++) stability=BalanceRules.AdvanceStability(stability,0.02f,true,0f,18f,0.8f);
  Check(stability==0.8f,"stationary target settles");
  Check(BalanceRules.AdvanceStability(stability,0.02f,true,19f,18f,0.8f)==0f,"fast angular crossing disrupts established tracking");
  Check(BalanceRules.AdvanceStability(stability,0.02f,false,0f,18f,0.8f)==0f,"native aim loss resets settling");
  Check(BalanceRules.AdvanceStability(0f,0.02f,true,18f,18f,0.8f)==0.01f,"near-limit settling is slower");
  Check(PresetCatalog.Get(ModPreset.Custom)==null,"Custom does not replace values");
  foreach(ModPreset preset in new[]{ModPreset.ModernDefense,ModPreset.Realistic,ModPreset.WornEquipment,ModPreset.ArcadeEasy}) {
   var values=PresetCatalog.Get(preset);
   Check(values.Count==40,"complete numeric preset");
   Check(values["TurretSlewMultiplier"]>0f && values["TurretSlewMultiplier"]==1f,"native turret slew restored");
   Check(values["RadarMinimumPlanningSeconds"]>0f,"radar zero-delay path covered");
   Check(values["RadarMaximumElevation"]==70f,"upper coverage retained");
  }
  var modern=PresetCatalog.Get(ModPreset.ModernDefense);var realistic=PresetCatalog.Get(ModPreset.Realistic);var worn=PresetCatalog.Get(ModPreset.WornEquipment);
  Check(modern["GunLeadErrorDegrees"]<realistic["GunLeadErrorDegrees"] && realistic["GunLeadErrorDegrees"]<worn["GunLeadErrorDegrees"],"preset strength ordering");
  Check(DefensePhysics.Classify(true,false,3000,1000,25000,1100)==DefenseRole.Gun,"gun role");
  Check(DefensePhysics.Classify(false,true,15000,800,25000,1100)==DefenseRole.PointDefense,"point role");
  Check(DefensePhysics.Classify(false,true,50000,1800,25000,1100)==DefenseRole.AreaDefense,"area role");
  Check(DefensePhysics.Classify(false,true,50000,900,25000,1100)==DefenseRole.PointDefense,"range alone is insufficient");
  Check(DefensePhysics.GunLeadError(280,3,1,330,2)==0f,"subsonic lead preserved");
  Check(DefensePhysics.GunLeadError(1500,3,1,330,2)>DefensePhysics.GunLeadError(700,3,1,330,2),"speed stresses lead");
  Check(DefensePhysics.GunLeadError(1500,3,1,330,2)>DefensePhysics.GunLeadError(1500,.2f,1,330,2),"flight time stresses lead");
  Check(DefensePhysics.GunLeadError(1500,3,1,330,2)>DefensePhysics.GunLeadError(1500,3,0,330,2),"crossing target more difficult");
  double intercept;
  Check(DefensePhysics.TryInterceptTime(100000000,-20000000,4000000,1500,out intercept) && Math.Abs(intercept-10000.0/3500)<1e-6,"head-on interception even for faster target");
  Check(!DefensePhysics.TryInterceptTime(100000000,20000000,4000000,1500,out intercept),"receding faster target unreachable");
  Check(DefensePhysics.CanEngage(100000000,-20000000,4000000,1500,.5,1.5,.57,4000,true),"area defense has head-on window");
  Check(!DefensePhysics.CanEngage(1000000,-2000000,4000000,600,1.8,1,.57,4000,false),"point defense cannot react in time");
  Check(!DefensePhysics.CanEngage(125000000,-20000000,4000000,1500,.5,1.5,.57,4000,true),"neighboring site outside footprint");
  Check(!DefensePhysics.CanEngage(100000000,20000000,4000000,1500,.5,1.5,.57,4000,true),"departing threat excluded");
  Check(!DefensePhysics.CanEngage(1000000,-2000000,4000000,1500,.1,1.5,.57,4000,true),"minimum flight time constrains late engagement");
  int rounds = DefensePhysics.CountConsumedRounds(0,6,5);
  Check(rounds==1,"first native launcher round counted");
  rounds=DefensePhysics.CountConsumedRounds(rounds,5,4);
  Check(rounds==2 && !DefensePhysics.HasLaunchBudget(rounds,2),"third simultaneous rail blocked");
  Check(DefensePhysics.CountConsumedRounds(rounds,4,4)==2,"unready launch consumes no budget");
  Check(DefensePhysics.HasLaunchBudget(rounds,0),"zero disables cap");
  Console.WriteLine("PASS: "+checks+" radar geometry, failure selection and motor timing assertions"); return 0;
 }
}
