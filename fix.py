import os

base_dir = r'e:\Projects\DungeonApp\Views'
for root, _, files in os.walk(base_dir):
    for file in files:
        if file.endswith('.axaml'):
            path = os.path.join(root, file)
            with open(path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            new_content = content.replace('Classes="accent"', 'Classes="primary"')
            new_content = new_content.replace('Classes="accent ', 'Classes="primary ')
            
            if new_content != content:
                with open(path, 'w', encoding='utf-8') as f:
                    f.write(new_content)
                print(f'Updated {file}')
