Scriptname SkyrimFairCrowdCull extends Quest
{The fair's crowd, switched off where the player can't see it. The fair has no occlusion,
so the game draws actors hidden behind stalls; switching them off saves their drawing and
their AI.

The generator works out, for each Spot-sized square of a grid over the fair, which actors
can be seen from anywhere within its margin (768: a sprint over one poll and a 3D load),
and writes that as Table: Words numbers a square, 31 actors to a number (Papyrus has no
bitwise operators, so a bit is read by division and every number stays positive). Every
Poll seconds this finds the player's square; when it changes, each actor whose bit
changed is enabled or disabled.

An actor is on only if it's seen and its crowd layer is on (SkyrimFairCrowdLayers), so
the layers still work: these actors have no enable parent, as a reference with one can't
be enabled by script. set SkyrimFairCrowdCulling to 0 turns every actor back on.
Written by the generator from fair.config.json (fairWorld.crowdCulling).}

WorldSpace Property FairWorld Auto
Actor[] Property Actors Auto
Int[] Property Layers Auto
{Each actor's crowd layer (0 is the first), or -1 for one outside the layers.}
Int[] Property Table Auto
Float Property OriginX Auto
Float Property OriginY Auto
Float Property Spot = 256.0 Auto
Int Property Columns Auto
Int Property Rows Auto
Int Property Words Auto
GlobalVariable Property CrowdLayer Auto
{How many crowd layers are on (the stage script's SkyrimFairCrowdLayers).}
GlobalVariable Property Culling Auto
{1: unseen actors are off. 0: every actor on (their layers still apply).}
Float Property Poll = 0.5 Auto
Float Property IdlePoll = 5.0 Auto

; The square applied (-1 everyone on, -2 nothing applied yet), and the layers it was
; applied with.
Int applied = -2
Int appliedLayers = -1
Bool ready = False
Int[] bit

Event OnInit()
	Setup()
	RegisterForSingleUpdate(2.0)
EndEvent

; Called by the player alias on every load: everything is applied afresh, in case the
; plugin (and its table) changed since the save.
Function Resync()
	Setup()
	applied = -2
	RegisterForSingleUpdate(1.0)
EndFunction

Function Setup()
	bit = new Int[31]
	Int b = 0
	Int v = 1
	While b < 31
		bit[b] = v
		If b < 30
			v *= 2
		EndIf
		b += 1
	EndWhile
	; If the table didn't load whole, leave everyone on.
	ready = Actors.Length > 0 && Layers.Length == Actors.Length && Words * 31 >= Actors.Length && Table.Length == Columns * Rows * Words
	Debug.Trace("SkyrimFairCrowdCull: " + Actors.Length + " actors, table " + Table.Length + " of " + (Columns * Rows * Words) + ", ready " + ready)
EndFunction

Event OnUpdate()
	Actor player = Game.GetPlayer()
	If player.GetWorldSpace() != FairWorld
		applied = -2
		RegisterForSingleUpdate(IdlePoll)
		Return
	EndIf

	Int layerCount = 1000
	If CrowdLayer
		layerCount = CrowdLayer.GetValue() as Int
	EndIf
	Int square = -1
	If ready && (!Culling || Culling.GetValue() >= 0.5)
		Int cx = Math.Floor((player.GetPositionX() - OriginX) / Spot)
		Int cy = Math.Floor((player.GetPositionY() - OriginY) / Spot)
		If cx < 0
			cx = 0
		ElseIf cx >= Columns
			cx = Columns - 1
		EndIf
		If cy < 0
			cy = 0
		ElseIf cy >= Rows
			cy = Rows - 1
		EndIf
		square = cy * Columns + cx
	EndIf
	If square != applied || layerCount != appliedLayers
		Apply(square, layerCount)
	EndIf
	RegisterForSingleUpdate(Poll)
EndEvent

; Only the actors whose bit changed are touched, unless everything must be applied.
Function Apply(Int square, Int layerCount)
	Bool all = applied == -2 || layerCount != appliedLayers
	Int switched = 0
	Int w = 0
	While w < Words
		Int now = WordAt(square, w)
		Int was = WordAt(applied, w)
		If all || now != was
			Int b = 0
			Int i = w * 31
			While b < 31 && i < Actors.Length
				Bool seen = (now / bit[b]) % 2 == 1
				If all || seen != ((was / bit[b]) % 2 == 1)
					If Set(i, seen && (Layers[i] < 0 || Layers[i] < layerCount))
						switched += 1
					EndIf
				EndIf
				b += 1
				i += 1
			EndWhile
		EndIf
		w += 1
	EndWhile
	applied = square
	appliedLayers = layerCount
	Debug.Trace("SkyrimFairCrowdCull: square " + square + ", layers " + layerCount + ", " + switched + " switched")
EndFunction

Int Function WordAt(Int square, Int w)
	If square < 0
		Return 2147483647
	EndIf
	Return Table[square * Words + w]
EndFunction

; True if the actor was switched.
Bool Function Set(Int i, Bool on)
	Actor a = Actors[i]
	If !a
		Return False
	EndIf
	If on && a.IsDisabled()
		a.EnableNoWait()
		Return True
	ElseIf !on && !a.IsDisabled()
		a.DisableNoWait()
		Return True
	EndIf
	Return False
EndFunction
