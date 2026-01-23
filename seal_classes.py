import os
import re

cs_files = []
for root, dirs, files in os.walk('.'):
    for f in files:
        if f.endswith('.cs'):
            cs_files.append(os.path.join(root, f))

inherited_types = set()
# Find what classes are inherited
inheritance_re = re.compile(r'(?:class|interface|record)\s+\w+(?:<[^>]+>)?\s*:\s*([^{]+)')
for f in cs_files:
    try:
        with open(f, 'r', encoding='utf-8') as file:
            content = file.read()
            # remove comments for safer inheritance parsing
            content_no_comments = re.sub(r'//.*|/\*.*?\*/', '', content, flags=re.DOTALL)
            for match in inheritance_re.finditer(content_no_comments):
                inheritance_list = match.group(1)
                for part in inheritance_list.split(','):
                    part = part.strip()
                    if not part: continue
                    base_name_match = re.match(r'^([A-Za-z_][A-Za-z0-9_]*)', part)
                    if base_name_match:
                        inherited_types.add(base_name_match.group(1))
    except Exception as e:
        print(f"Error reading {f}: {e}")

# Now seal uninherited classes
for f in cs_files:
    try:
        with open(f, 'r', encoding='utf-8') as file:
            content = file.read()
        
        new_content = content
        modifications = []
        
        # Match lines like "public class X", "internal partial class Y", etc.
        # Ensure we don't match partial words or inside comments easily by using line anchors
        class_regex = re.compile(r'^([ \t]*)(public|internal)(?:\s+partial)?\s+(class)\s+([A-Za-z_][A-Za-z0-9_]*)', re.MULTILINE)
        
        for match in class_regex.finditer(content):
            full_line_match = match.group(0)
            class_name = match.group(4)
            
            # Check preceding modifiers on the same logical line
            # We will just look at the line up to the word 'class'
            line_start = content.rfind('\n', 0, match.start())
            if line_start == -1: line_start = 0
            line = content[line_start:match.start() + len(full_line_match)]
            
            if 'sealed ' in line or 'abstract ' in line or 'static ' in line:
                continue
                
            if class_name not in inherited_types and class_name not in ['Program']:
                # Replace 'class ' with 'sealed class '
                modifications.append((match.start(3), match.end(3), 'sealed class'))
                
        if modifications:
            for start, end, replacement in reversed(modifications):
                new_content = new_content[:start] + replacement + new_content[end:]
            with open(f, 'w', encoding='utf-8') as file:
                file.write(new_content)
    except Exception as e:
        print(f"Error processing {f}: {e}")
