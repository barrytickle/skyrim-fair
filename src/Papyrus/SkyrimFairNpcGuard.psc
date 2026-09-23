Scriptname SkyrimFairNpcGuard extends Actor
{On every fair NPC. As each one loads, it takes off the spells in StripSpells: other
mods' abilities that, at the fair's density, flood Papyrus (a detection cloak on every
NPC, a script that runs on every effect applied). The stage quest fills the list from
the plugins that are loaded, so the fair needs none of them as masters.}

FormList Property StripSpells Auto

Event OnLoad()
	If !StripSpells
		Return
	EndIf
	Int i = StripSpells.GetSize()
	While i > 0
		i -= 1
		Spell s = StripSpells.GetAt(i) as Spell
		If s && HasSpell(s)
			Bool removed = RemoveSpell(s)
			Debug.Trace("SkyrimFairGuard: " + self + " " + s + " removed " + removed + ", still has it " + HasSpell(s))
		EndIf
	EndWhile
EndEvent
