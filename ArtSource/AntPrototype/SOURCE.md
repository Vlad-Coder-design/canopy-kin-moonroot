# Formica rufa player prototype source

- Chosen species: `Formica rufa` (red wood ant).
- Editable source:
  `ArtSource/AntPrototype/CanopyKin_FormicaRufa_Player_Prototype.blend`
- Unity model:
  `Assets/Resources/Models/Ant/Prototype/CanopyKin_FormicaRufa_Player_Prototype.fbx`
- Build tool: `Tools/build_formica_rufa_prototype.py`
- Donor geometry, rig and authored motion:
  `Game-Ready Worker Ant Model` by Msassasa (`@LilCick`), CC BY 4.0.
- Donor source record:
  `ArtSource/ThirdParty/Sketchfab/GameReadyWorkerAnt/SOURCE.md`
- Project-owned surface inputs:
  `Assets/Resources/HighQuality/Original/Ant/ant_exoskeleton_*_4k.*`

## Why Formica rufa

The mission is set on a European-style forest floor and revolves around a
large, territorial mound-building colony. `Formica rufa` is a common red wood
ant associated with woodland habitat. The prototype follows its characteristic
bicolored worker appearance: reddish head/mesosoma and petiolar scale with a
dark gaster. AntWeb identifies the species as a woodland ant with workers
around 8–10 mm; the 2025 red-wood-ant literature review gives a broader worker
range of approximately 5–10 mm.

Biological visual references (no reference image is redistributed):

- https://www.antweb.org/description.do?genus=formica&species=rufa&subfamily=formicinae
- https://pmc.ncbi.nlm.nih.gov/articles/PMC12111979/
- https://artsdatabanken.no/arter/takson/77460/beskrivelse

## Project modifications

- Baked the donor's 53 rigid anatomical pieces to a single identity-transform
  skinned mesh while preserving the editable donor unchanged.
- Retained the detailed head, compound eyes, thoracic shell, gaster, mandibles,
  six coxae/femora/tibiae and all source UVs.
- Replaced 1,792 disconnected donor vertices in the distal antenna/tarsus
  chains with continuous, closed, bone-weighted shells.
- Added an explicit one-segment Formicinae petiole represented by a high,
  laterally broad, anteroposteriorly thin scale.
- Added two independently weighted pretarsal claws to each of six feet.
- Added deterministic, slightly asymmetric macro setae.
- Added recessed arthrodial membrane rings at rigid exoskeleton joints.
- Configured separate red mesosoma, dark gaster, dark joint and compound-eye
  PBR material regions.
- Connected project-owned 4K albedo, DirectX normal, roughness and AO sources,
  plus procedural cuticle microstructure in the editable Blender material.
- Transferred 17 dense donor actions to clean production bone names without
  replacing their authored curves with code-generated gait placeholders.
- Added seven clearly marked time-remapped variants for slow/run/backward,
  alert/explore and in-place turn prototype testing.
- Preserved a hidden, editable 304,764-triangle high-poly bake source; it is
  not exported to Unity.

## Current artifact identity

- Blender SHA-256:
  `91E27A846EE438CE7E5BFDA7246CF57798D3C5EEC4432C08951D5C31B271E8BF`
- FBX SHA-256:
  `254ED9E9601B419ED6AC9DD8C0199CF83BF2563DFFAC391344777F42E231F194`
- Runtime FBX topology: 53,394 triangles.
- High-poly editable topology: 304,764 triangles.
- Armature: 67 bones.
- Exported animation actions: 24.

The prototype has passed repeated Blender render inspection. Unity import and
Game View approval are intentionally not marked complete until Unity Hub has a
valid local license and `AntPrototypeSceneBuilder.BuildAndValidate` succeeds.
