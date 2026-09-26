;BEGIN FRAGMENT CODE - Do not edit anything between this and the end comment
Scriptname SkyrimFairCameoLine Extends TopicInfo Hidden
{The begin fragment on each of the cameos' lines (FairCameos.cs): the stage music is ducked
until the line has been said. A greeting from an NPC with no topics never opens the dialogue
menu, so the stage script can't see it (IsInDialogueWithPlayer stays false); this tells it.}

;BEGIN FRAGMENT Fragment_0
Function Fragment_0(ObjectReference akSpeakerRef)
;BEGIN CODE
	DuckUntil.SetValue(Utility.GetCurrentRealTime() + Seconds)
	If Passport && Stamp > 0
		(Passport as SkyrimFairPassport).Stamp(Stamp)
	EndIf
;END CODE
EndFunction
;END FRAGMENT

;END FRAGMENT CODE - Do not edit anything between this and the begin comment

GlobalVariable Property DuckUntil Auto
{Real time (Utility.GetCurrentRealTime) until which the stage script ducks the music.}
Float Property Seconds Auto
{This line's length, and a little.}
Quest Property Passport Auto
{The Fair Passport (SkyrimFairPassport), if built.}
Int Property Stamp Auto
{The Passport objective this cameo's lines stamp (20 Garrick; 0 none, as Claudius's: his stamp, 30, is retired).}
