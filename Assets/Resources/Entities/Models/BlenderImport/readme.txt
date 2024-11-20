HOW TO CREATE NEW ASSET MODELS

In Blender:
-----------------------------
- Assets are made in blender
- 1 Unit in Blender equals 1 unit/tile/node in Unity
- When pressing Numpad-7 in blender, the bottom left corner is the SW (0/0) corner on the entity in default (N) rotation.
- Origin point needs to be 0/0/0 point (Shift+S > Cursor to World Origin)
- Models need to be centered in the x/y axis and go upwards in the blender-z-axis (except if they should clip through the ground)
- Create materials and assign faces to the correct materials
	- Add textures to the materials and correctly map the uv
	- All textures need to be in the Unity project directory!

- Before saving, remove the camera and light in the scene
- Before saving select object > press Ctrl+A > All transforms
- Save file as .blend for future edits
- Export file as .fbx to use in Unity 
	> (VERY IMPORTANT: Set the chechbox "Apply Transform" when exporting)

- Check log_2x1_blend.blend as reference

In Unity:
-----------------------------
- Go To Resources/Entities/Models/BlenderImport/... to your saved path
- Select the _fbx file, go to 'Materials' and select 'Extract Materials...' in the same folder
- Change the shader of the extracted materials to EntityShader
- Set the Color as a color similar to the texture (just pick with Pipette)
	- or alternatively if entity only supports flat shading, check "Only Flat Shading"