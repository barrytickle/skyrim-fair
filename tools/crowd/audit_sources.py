import sys,json,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
WORK=ROOT/'build'/'crowd'/'Clapping01'
WORK.mkdir(parents=True,exist_ok=True)
NIFTOOLS=Path(r'C:\Users\Barry\Downloads\Skyrim Actors\crowd-tools\pynifly\io_scene_nifly')
sys.path.insert(0,str(NIFTOOLS));sys.path.insert(0,str(ROOT/'tools'))
from pyn.pynifly import NifFile
import bsa_extract
SOURCE=Path(r'C:\Users\Barry\Downloads\Skyrim Clothing\meshes')
DATA=Path(r'E:\SteamLibrary\steamapps\common\Skyrim Special Edition\Data')
FILES=[
 ('outfit','clothes/farmclothes01/torsom_1.nif'),
 ('boots','clothes/farmclothes01/bootsm_1.nif'),
 ('hat','clothes/farmclothes01/hatm.nif'),
 ('hands','actors/character/character assets/malehands_1.nif'),
 ('head','actors/character/character assets/malehead.nif'),
 ('eyes','actors/character/character assets/eyesmale.nif'),
 ('brows','actors/character/character assets/faceparts/malebrows.nif'),
 ('mouth','actors/character/character assets/mouth/mouthhuman.nif'),
]
def main():
    print('Indexing stock texture archives',flush=True)
    index=bsa_extract.build_index(str(DATA));report={'meshes':[],'textures':{},'missing_textures':[]}
    for role,rel in FILES:
        src=SOURCE/rel;nif=NifFile(str(src))
        entry={'role':role,'source':str(src),'sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'shapes':[]}
        for shape in nif.shapes:
            entry['shapes'].append({'name':shape.name,'vertices':len(shape.verts),'triangles':len(shape.tris),
                'textures':shape.textures,'shader_type':shape.shader.properties.Shader_Type,
                'flags1':shape.shader.properties.Shader_Flags_1,'flags2':shape.shader.properties.Shader_Flags_2,
                'bones':shape.bone_names})
            for tex in shape.textures.values():
                if not tex:continue
                key=tex.replace('\\','/').lower()
                if key in report['textures']:continue
                dest=WORK/'preview_resources'/key
                if key in index:
                    dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(bsa_extract.extract(index[key]))
                    report['textures'][key]={'preview_path':str(dest),'archive':str(index[key][0])}
                elif (DATA/key).exists():
                    report['textures'][key]={'preview_path':str(DATA/key),'loose':True}
                else:report['missing_textures'].append(key)
        report['meshes'].append(entry)
    (WORK/'source_audit.json').write_text(json.dumps(report,indent=2))
    print(json.dumps({'meshes':len(report['meshes']),'textures':len(report['textures']),'missing':report['missing_textures']},indent=2))
if __name__=='__main__':main()
