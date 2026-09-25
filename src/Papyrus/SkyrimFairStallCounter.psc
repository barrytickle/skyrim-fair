Scriptname SkyrimFairStallCounter extends ObjectReference
{A stall's counter (FairShops.cs): an activator with the counter's own model, named for the
stall. The keepers stand behind deep counters and tall goods hide them, so from the street
they could be out of reach (2026-09-25). Using the counter opens its keeper's barter menu.}

ActorBase Property Keeper Auto
{The keeper's record; the nearest of its references is the one served.}
Float Property Reach = 280.0 Auto
{How far from the counter the keeper may be.}

Event OnActivate(ObjectReference akActionRef)
	If akActionRef != Game.GetPlayer()
		Return
	EndIf
	Actor k = Game.FindClosestReferenceOfTypeFromRef(Keeper, Self, Reach) as Actor
	If k && !k.IsDead() && k.Is3DLoaded()
		k.ShowBarterMenu()
	EndIf
EndEvent
