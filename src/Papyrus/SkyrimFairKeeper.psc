Scriptname SkyrimFairKeeper extends Actor
{A stall keeper (FairVendors.cs, FairShops.cs). The spot behind the counter is boxed in by the
stall's goods and crates, so a keeper pushed out of it can't walk back (the clothing stall,
2026-09-25). A moment after loading, and now and then while loaded, one who has strayed is put
back at their spot, unless the player is talking to them.}

Float Property HomeX Auto
Float Property HomeY Auto
{Their spot (the reference's position, set by the generator).}
Float Property Stray = 60.0 Auto
{Further than this from their spot, they're put back.}

Event OnLoad()
	RegisterForSingleUpdate(3.0)
EndEvent

Event OnUpdate()
	If !Is3DLoaded()
		Return
	EndIf
	Float dx = GetPositionX() - HomeX
	Float dy = GetPositionY() - HomeY
	If dx * dx + dy * dy > Stray * Stray && !IsInDialogueWithPlayer() && !IsInCombat()
		MoveToMyEditorLocation()
	EndIf
	RegisterForSingleUpdate(20.0)
EndEvent
