# Moonroot visible-asset audit — 0.7.0 baseline

The screenshots in `QA/Screenshots` and the published 0.7.0 build were inspected
at player-camera distance. This is an acceptance audit, not a claim that an item
is production-ready.

| Visible category | 0.7.0 problem observed | Required replacement / acceptance evidence |
|---|---|---|
| Forest ground | Broad, visibly tiled, uniformly shiny plane; material detail does not match ant scale | Multi-scale dry/damp soil with broken repetition, sculpted clods/depressions and close debris; same-angle ground screenshot |
| Grass | Dense walls of thin alpha ribbons; repeated silhouette blocks the player | Fewer compositionally placed species, curved/thick support geometry, readable gaps and camera corridor; close and wide screenshots |
| Living leaves | Mostly grass-only forest; no convincing low groundcover hierarchy | Sedge, wood sorrel, serrated seedlings and creeping plants in ecological clusters |
| Fallen leaves | Atlas detail is better than the old ribbon but leaves still sit as cards on the soil | Stronger curl, thickness, midrib/stem, partial burial and varied decay zones |
| Roots and trunks | Overscaled cut poles create an artificial stockade and reveal empty sky/map edge | Root-flared landmarks, continuous silhouettes outside the frame, branches/tunnels instead of repeated vertical columns |
| Stones | Triangle-split normals make obvious faceted blobs | Smooth weathered base normals with a small number of intentional chipped planes and partial burial |
| Moss | Rounded green cushions read as stones recolored green | Irregular edge growth, multiple hummock scales, strand/tuft breakup and sheltered placement |
| Water | Single blue glossy patch without a convincing muddy edge | Shallow depression, wet-bank ring, surface-tension edge and restrained reflection |
| Seeds | One elongated generic brood mesh recolored brown | Ridged seed coat, hilum, point, optional awn/cap and readable species variations |
| Resin | One generic smooth orange blob | Fused irregular droplets, entrained debris/bubbles and controlled translucent amber material |
| Protein food | One recolored smooth capsule | Distinct insect-remain fragments: chitin plate, leg segment and soft tissue crumb |
| Eggs | Same capsule as every other brood item | Small clustered ovoid eggs with translucent shell variation |
| Larvae | Same smooth capsule stretched | Segmented curved body, head end, folds and non-uniform profile |
| Pupae | Same smooth capsule stretched | Cocoon/pupal silhouette with thorax/head/limb impressions and matte fibrous surface |
| Queen chamber | Queen and capsules sit on one raised dark oval; the whole colony reads as one room | Irregular nursery basin, packed-soil berms, root alcove, chamber-to-tunnel composition and visible job zones |
| Tunnels / nest | One circular shell, radial root spikes and a black ellipse entrance | Organic connected chamber mouths and tunnel collars; no rectangular/black void framing; lit depth gradient |
| Underground light | Large areas clip to near-black; ant silhouette loses anatomy | Warm indirect fill plus cool entrance bounce, readable darks and preserved contact shadows |
| Surface light / background | Harsh contrast, blown sky gaps and black ant silhouette | Softer macro-canopy key/fill, fogged vegetated perimeter and readable exoskeleton highlights |

The first implementation stage replaces brood/resources and the underground
colony geometry because the queen briefing is the first player view and currently
contains the most obvious repeated placeholder mesh. The forest stage follows in
the order listed above. No pre-existing import `.meta` changes are discarded.
