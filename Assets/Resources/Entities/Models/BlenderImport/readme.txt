HOW TO CREATE NEW ASSET MODELS

- Assets are made in blender
- 1 Unit in Blender equals 1 unit/tile/node in Unity
- Origin point needs to be 0/0/0 point
- Models need to be centered in the x/y axis and go upwards in the blender-z-axis (except if they should clip through the ground)
- Create dummy materials and assign faces to the correct materials
	- materials can then be remapped in unity editor with unity materials that use EntityShader

- Before saving select object > press Ctrl+A > All transforms
- Save file as .blend for future edits
- Export file as .fbx to use in Unity (VERY IMPORTANT: Set the chechbox "Apply Transform" when exporting)

- Check log_2x1_blend.blend as reference