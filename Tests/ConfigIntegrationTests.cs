using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using BepInEx.Configuration;
class ConfigIntegrationTests
{
 static int checks;
 static void Check(bool condition,string message) { checks++;if(!condition)throw new Exception(message); }
 static int Main(string[] args)
 {
  string game=args.Length>0?args[0]:@"F:\Games\Nuclear.Option.v0.34.1";
  AppDomain.CurrentDomain.AssemblyResolve+=(sender,e)=>{
   foreach(string root in new[]{Path.Combine(game,"NuclearOption_Data","Managed"),Path.Combine(game,"BepInEx","core")}) {
    string path=Path.Combine(root,new AssemblyName(e.Name).Name+".dll");if(File.Exists(path))return Assembly.LoadFrom(path);
   }return null;
  };
  return Run();
 }
 static int Run()
 {
  var assembly=typeof(RevertToStoneAge.ModPreset).Assembly;
  Type plugin=assembly.GetType("RevertToStoneAge.RevertToStoneAgePlugin",true);
  Type preset=assembly.GetType("RevertToStoneAge.ModPreset",true);
  object instance=FormatterServices.GetUninitializedObject(plugin);
  string path=Path.Combine(Path.GetTempPath(),"RevertToStoneAge-"+Guid.NewGuid()+".cfg");
  var config=new ConfigFile(path,false);
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  plugin.BaseType.GetField("<Config>k__BackingField",flags).SetValue(instance,config);
  object selected=config.Bind<RevertToStoneAge.ModPreset>("0. General","Preset",RevertToStoneAge.ModPreset.Realistic);
  plugin.GetField("ActivePreset").SetValue(null,selected);
  foreach(var field in plugin.GetFields(BindingFlags.Public|BindingFlags.Static)) {
   if(field.FieldType==typeof(ConfigEntry<float>))field.SetValue(null,config.Bind<float>("Numeric",field.Name,0.5f));
   if(field.FieldType==typeof(ConfigEntry<int>))field.SetValue(null,config.Bind<int>("Numeric",field.Name,2));
   if(field.FieldType==typeof(ConfigEntry<bool>))field.SetValue(null,config.Bind<bool>("Toggles",field.Name,true));
  }
  var handler=(EventHandler<SettingChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<SettingChangedEventArgs>),instance,plugin.GetMethod("OnSettingChanged",flags));
  config.SettingChanged+=handler;
  plugin.GetMethod("ApplySelectedPreset",flags).Invoke(instance,null);
  var value=(ConfigEntry<RevertToStoneAge.ModPreset>)selected;
  foreach(var choice in new[]{RevertToStoneAge.ModPreset.WornEquipment,RevertToStoneAge.ModPreset.ModernDefense,RevertToStoneAge.ModPreset.ArcadeEasy,RevertToStoneAge.ModPreset.Realistic}) {
   value.Value=choice;
   foreach(var pair in RevertToStoneAge.PresetCatalog.Get(choice)) {
    var entry=(ConfigEntry<float>)plugin.GetField(pair.Key).GetValue(null);
    Check(entry.Value==pair.Value,"visible numeric preset value: "+pair.Key);
   }
   Check(value.Value==choice,"no recursive conversion to Custom");
   Check((float)plugin.GetField("CachedLockTimeMult").GetValue(null)==((ConfigEntry<float>)plugin.GetField("TurretLockTimeMultiplier").GetValue(null)).Value,"runtime cache matches UI");
  }
  var reaction=(ConfigEntry<float>)plugin.GetField("GunTargetSwitchDelay").GetValue(null);
  reaction.Value=2.7f;
  Check(value.Value==RevertToStoneAge.ModPreset.Custom,"numeric edit selects Custom");
  Check(reaction.Value==2.7f,"numeric edit retained");
  value.Value=RevertToStoneAge.ModPreset.Realistic;
  value.Value=RevertToStoneAge.ModPreset.Custom;
  Check(reaction.Value==0.3f,"Custom retains prior visible values");
  var toggle=(ConfigEntry<bool>)plugin.GetField("TurretDelayEnabled").GetValue(null);
  value.Value=RevertToStoneAge.ModPreset.Realistic;toggle.Value=false;
  Check(value.Value==RevertToStoneAge.ModPreset.Realistic,"feature toggle is independent");
  config.Save();
  var reread=new ConfigFile(path,true);
  Check(reread.Bind<RevertToStoneAge.ModPreset>("0. General","Preset",RevertToStoneAge.ModPreset.Custom).Value==RevertToStoneAge.ModPreset.Realistic,"preset saved to disk");
  Check(reread.Bind<float>("Numeric","GunTargetSwitchDelay",0f).Value==0.3f,"numeric value saved to disk");
  File.Delete(path);
  Console.WriteLine("PASS: "+checks+" actual BepInEx preset/config event assertions");return 0;
 }
}
