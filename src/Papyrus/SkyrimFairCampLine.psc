;BEGIN FRAGMENT CODE - Do not edit anything between this and the end comment
Scriptname SkyrimFairCampLine Extends TopicInfo Hidden
{The begin fragment on Claudius's camp and schedule lines (FairCamp.cs): the music is ducked
while he speaks, as on his other lines, and the controller (SkyrimFairCamp) acts on the line:
Step 1 bring the fair back, 2 skip the story and bring it back, 3 every day, 4 only after the
main story, 5 the story's end remarked on; 0 nothing.}

;BEGIN FRAGMENT Fragment_0
Function Fragment_0(ObjectReference akSpeakerRef)
;BEGIN CODE
	DuckUntil.SetValue(Utility.GetCurrentRealTime() + Seconds)
	If Step > 0
		(Camp as SkyrimFairCamp).Said(Step)
	EndIf
;END CODE
EndFunction
;END FRAGMENT

;END FRAGMENT CODE - Do not edit anything between this and the begin comment

GlobalVariable Property DuckUntil Auto
{Real time (Utility.GetCurrentRealTime) until which the stage script ducks the music.}
Float Property Seconds Auto
{This line's length, and a little.}
Quest Property Camp Auto
Int Property Step Auto
