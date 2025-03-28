# Recalculate Blendshape Normals
Non-modular version of [Hai~vr](https://github.com/hai-vr/)'s blendshape normal recalculator. Meant for avatar artists who want to reduce project dependencies and customer friction. 

# Add to VRChat Creator Companion
https://rykertm.github.io/vpm-listing/


## Prepare your FBX first...
Enable 'Legacy Blend Shapes Normals' or set 'Blend Shape Normals' to 'None.' 

## Access the tool from the top toolbar 
Tools > RykerTM > Normal Calculator > Configure Import Settings

Save the config.json to the avatar or model folder. If you need to edit an existing config, load it on the bottom of the tool.

## Things to know:
- Blend shapes are recalculated when a configured FBX is reimported or the project is initialized. 
- You can disable this script from the toolbar 'Tools > RykerTM > Normal Calculator > Recalculate On Import.' Any normal calculations are undone when the FBX is reimported or overwritten when disabled.
- This script can load and recalculate from multiple config.jsons. This is so multiple avatars and models can coexist on a single project.
