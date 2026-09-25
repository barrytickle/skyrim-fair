Scriptname SkyrimFairRoofHorse extends Actor
{The horse on the stage roof (FairCameos.cs), on its plank platform across two rafters.
Nobody knows how it got up there. It stays: it can't be ridden down, and it never walks off
the edge (there's no navmesh up there, so its package alone would try to path away).
An actor can load before the planks' collision does and drop through (it vanished, 2026-09-25),
so a moment after each load, and every so often while loaded, it's put back where it belongs.}

Event OnLoad()
	BlockActivation(True)
	SetDontMove(True)
	RegisterForSingleUpdate(2.0)
EndEvent

Event OnUpdate()
	If !Is3DLoaded()
		Return
	EndIf
	; Off its planks (fallen, or pushed): back to its editor location.
	If Math.Abs(GetPositionZ() - HomeZ) > 40.0 || Math.Abs(GetPositionX() - HomeX) > 60.0 || Math.Abs(GetPositionY() - HomeY) > 60.0
		SetDontMove(False)
		MoveToMyEditorLocation()
		SetDontMove(True)
	EndIf
	RegisterForSingleUpdate(15.0)
EndEvent

Float Property HomeX Auto
Float Property HomeY Auto
Float Property HomeZ Auto
{Where it stands on the planks (set by the generator from the reference).}
