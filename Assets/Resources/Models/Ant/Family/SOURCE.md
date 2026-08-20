# Canopy Kin 0.5.1 upright ant family

- Generator: `Tools/build_upright_ant_family.py`
- External anatomical and rigging base:
  `ArtSource/ThirdParty/Sketchfab/GameReadyWorkerAnt/Raw/GameReadyWorkerAnt.blend`
- License/provenance:
  `ArtSource/ThirdParty/Sketchfab/GameReadyWorkerAnt/SOURCE.md`
- Project source files: `ArtSource/AntFamily/CanopyKinAnt_*.blend`
- Runtime files: `CanopyKinAnt_*.fbx`
- Caste exports: Player, Scout, Worker, Nurse, LightSoldier, HeavySoldier,
  Queen and Rival

Each FBX contains:

- one clean 52-bone production armature with six anatomically segmented legs,
  two three-stage antenna controls, two mandibles, head, thorax and abdomen;
- one 47,090-triangle close-camera skinned LOD;
- one 11,772-triangle distant skinned LOD;
- UVs and material regions for the PBR shell, flexible joints and compound
  eyes;
- thirteen actions: Idle, Walk, Run, StartMove, StopMove, TurnLeft,
  TurnRight, Attack, Carry, Interact, Climb, Stagger and Death.

The selected Sketchfab source was inspected in Blender before use. The 53
bone-parented ant segments were baked into one identity-transform skinned mesh.
Nine open segment caps were closed, all faces were triangulated, and all face
normals were recalculated outward. The anatomical convention is baked into the
FBX as Unity `+Y` up and `+Z` forward, so `AntVisual` instantiates every caste
with identity local rotation. No corrective per-instance model rotation is
required.

Unity replaces the imported source material with the project's opaque PBR
cuticle and compound-eye materials. Runtime setup explicitly disables alpha
blend, alpha test, premultiply and LOD cross-fade keywords, writes depth, and
uses back-face culling.
