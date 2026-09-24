Scriptname SkyrimFairAudioPlayerAlias extends ReferenceAlias
{On the fair audio quest's player alias: sounds are gone after a load, so the stage set
starts over cleanly, and the crowd culling applies itself afresh.}

Event OnPlayerLoadGame()
	(GetOwningQuest() as SkyrimFairAudioScript).Recover()
	SkyrimFairCrowdCull cull = GetOwningQuest() as SkyrimFairCrowdCull
	If cull
		cull.Resync()
	EndIf
EndEvent
