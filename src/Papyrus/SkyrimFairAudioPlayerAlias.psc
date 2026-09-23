Scriptname SkyrimFairAudioPlayerAlias extends ReferenceAlias
{On the fair audio quest's player alias: sounds are gone after a load, so the stage set
starts over cleanly.}

Event OnPlayerLoadGame()
	(GetOwningQuest() as SkyrimFairAudioScript).Recover()
EndEvent
