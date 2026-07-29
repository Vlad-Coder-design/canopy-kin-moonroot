# Canopy Kin 0.5 ant family

- Generator: `Tools/build_ant_family.py`
- External anatomical base:
  `ArtSource/ThirdParty/OpenGameArt/Ant/Raw/ant.blend`
- License/provenance:
  `ArtSource/ThirdParty/OpenGameArt/Ant/SOURCE.md`
- Project source files: `ArtSource/AntFamily/CanopyKinAnt_*.blend`
- Runtime files: `CanopyKinAnt_*.fbx`
- Caste exports: Player, Scout, Worker, Nurse, LightSoldier, HeavySoldier,
  Queen and Rival

Each FBX contains:

- one repaired 39-bone armature with six four-segment legs, two three-segment
  antennae, two independently animated mandibles, head, thorax and abdomen;
- one approximately 105K-triangle close-camera skinned LOD;
- one approximately 6.6K-triangle distant skinned LOD;
- UVs and material regions for the PBR shell, flexible joints and compound
  eyes;
- thirteen actions: Idle, Walk, Run, StartMove, StopMove, TurnLeft,
  TurnRight, Attack, Carry, Interact, Climb, Stagger and Death.

The source OpenGameArt file was inspected before use. Its original 1,080
triangles, missing packed textures, unnamed hierarchy, six-influence weights
and two actions were insufficient for direct runtime use. The low-detail
original is retained only for legal provenance and is not instantiated by
`AntVisual`.
