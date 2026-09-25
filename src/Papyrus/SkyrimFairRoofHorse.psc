Scriptname SkyrimFairRoofHorse extends Actor
{The horse on the stage roof (FairCameos.cs), on its plank platform across two rafters.
Nobody knows how it got up there. It stays: it can't be ridden down, and it never walks off
the edge (there's no navmesh up there, so its package alone would try to path away).}

Event OnLoad()
	BlockActivation(True)
	SetDontMove(True)
EndEvent
