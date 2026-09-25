;BEGIN FRAGMENT CODE - Do not edit anything between this and the end comment
Scriptname SkyrimFairTalkDuck Extends Perk Hidden
{The fair's talk perk's fragment (FairPluginGenerator, fairWorld.talkDuck). Pressing E on anyone
at the fair (not Garrick or Claudius, whose own lines duck the music; not while sneaking; not
the dead) runs this instead of the plain activation: the stage music is ducked for a few
seconds (DuckUntil, the real time it ends), then the normal activation runs, so the
conversation, greeting or shop happens as usual. A greeting opens no menu, so without this the
stage script can't tell anyone is speaking (a Nexus player couldn't hear the NPCs).}

;BEGIN FRAGMENT Fragment_0
Function Fragment_0(ObjectReference akTargetRef, Actor akActor)
;BEGIN CODE
	DuckUntil.SetValue(Utility.GetCurrentRealTime() + Seconds)
	akTargetRef.Activate(akActor, True)
;END CODE
EndFunction
;END FRAGMENT

;END FRAGMENT CODE - Do not edit anything between this and the begin comment

GlobalVariable Property DuckUntil Auto
{Real time (Utility.GetCurrentRealTime) until which the stage script ducks the music.}
Float Property Seconds Auto
{How long a reply is given (5).}
