Scriptname SkyrimFairStallCounter extends ObjectReference
{A stall's counter (FairShops.cs): an activator with the counter's own model, named for the
stall. The keepers stand behind deep counters and tall goods hide them, so from the street
they could be out of reach (2026-09-25). Using the counter opens its keeper's barter menu.}

ActorBase Property Keeper Auto
{Retired: a keeper's reference runs on a copy of this record made at load (their faces come
from a leveled list), so a search by it found nobody (2026-09-25). KeeperRefId is used now.}
Int Property KeeperRefId Auto
String Property KeeperPlugin Auto
{The keeper's own reference (its FormID in the plugin), looked up when the counter is used.}
Float Property Reach = 280.0 Auto
{How far from the counter the keeper may be.}

Event OnActivate(ObjectReference akActionRef)
	If akActionRef != Game.GetPlayer()
		Return
	EndIf
	Actor k = None
	If KeeperRefId > 0 && KeeperPlugin != ""
		k = Game.GetFormFromFile(KeeperRefId, KeeperPlugin) as Actor
	EndIf
	If !k
		Debug.Trace("SkyrimFairStallCounter: no keeper for " + Self + " (" + KeeperRefId + ")")
		Return
	EndIf
	If k && !k.IsDead() && k.Is3DLoaded()
		k.ShowBarterMenu()
	EndIf
EndEvent
