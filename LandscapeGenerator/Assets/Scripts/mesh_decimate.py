import bpy
import os

# Ruta del directorio de modelos 3D
base_dir = "C:\\Users\\rafik\\Modelos 3D"

# Crear carpetas de salida
output_dirs = {
    "midpoly": os.path.join(base_dir, "midpoly"),
    "lowpoly": os.path.join(base_dir, "lowpoly"),
    "superlowpoly": os.path.join(base_dir, "superlowpoly")
}

for dir_name in output_dirs.values():
    if not os.path.exists(dir_name):
        os.makedirs(dir_name)

# Función para reducir el número de polígonos usando Spark AR Toolkit
def reduce_polygons(obj, ratio, output_path):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    
    # Aplicar el modificador de reducción de polígonos de Spark AR Toolkit
    bpy.ops.object.modifier_add(type='DECIMATE')
    decimate_modifier = obj.modifiers[-1]
    decimate_modifier.ratio = ratio
    
    # Aplicar el modificador
    bpy.ops.object.modifier_apply(modifier=decimate_modifier.name)
    
    # Guardar el archivo
    bpy.ops.export_scene.obj(filepath=output_path)
    print(f"Saved {output_path}")

# Procesar cada archivo en el directorio
for file_name in os.listdir(base_dir):
    if file_name.endswith(".obj"):  # Procesar solo archivos OBJ
        file_path = os.path.join(base_dir, file_name)
        
        # Importar el modelo 3D
        bpy.ops.import_scene.obj(filepath=file_path)
        obj = bpy.context.selected_objects[0]
        
        # Generar versiones con diferentes resoluciones
        for label, ratio in [("midpoly", 0.5), ("lowpoly", 0.25), ("superlowpoly", 0.1)]:
            output_path = os.path.join(output_dirs[label], file_name)
            reduce_polygons(obj, ratio, output_path)
        
        # Eliminar el objeto importado para limpiar la escena
        bpy.ops.object.delete()

print("Proceso completado.")
