;BEGIN FRAGMENT CODE - Do not edit anything between this and the end comment
Scriptname SkyrimFairPassportLine Extends TopicInfo Hidden
{The begin fragment on Claudius's two Passport lines (FairPassport.cs): the music is ducked
while he speaks, as on his other lines, and the Passport is handed over (Step 1) or handed
back for the reward (Step 2).}

;BEGIN FRAGMENT Fragment_0
Function Fragment_0(ObjectReference akSpeakerRef)
;BEGIN CODE
	DuckUntil.SetValue(Utility.GetCurrentRealTime() + Seconds)
	(Passport as SkyrimFairPassport).Hand(Step)
;END CODE
EndFunction
;END FRAGMENT

;END FRAGMENT CODE - Do not edit anything between this and the begin comment

GlobalVariable Property DuckUntil Auto
{Real time (Utility.GetCurrentRealTime) until which the stage script ducks the music.}
Float Property Seconds Auto
{This line's length, and a little.}
Quest Property Passport Auto
Int Property Step Auto
{1 the hand-over, 2 the hand-in.}
