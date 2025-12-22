// --- 1. الإعداد الأساسي للمشهد ---
const container = document.getElementById('viewer');
const scene = new THREE.Scene();
let width = container.clientWidth;
let height = container.clientHeight;
let aspect = width / height;
const frustumSize = 100;

const camera = new THREE.OrthographicCamera(
    frustumSize * aspect / -2, frustumSize * aspect / 2,
    frustumSize / 2, frustumSize / -2,
    0.1, 1000
);
camera.position.set(0, 0, 100);

const renderer = new THREE.WebGLRenderer({ antialias: true });
renderer.setSize(width, height);
container.appendChild(renderer.domElement);
scene.background = new THREE.Color(0x222222);

let linetypes = {};
const objectsToIntersect = [];
const raycaster = new THREE.Raycaster();
const pointer = new THREE.Vector2();
let selectedMesh = null;
let zoom = 1;
let minZoom = 0.1;
let isPanning = false;
let panStart = { x: 0, y: 0 };
let panOffset = { x: 0, y: 0 };

function animate() {
    requestAnimationFrame(animate);
    renderer.render(scene, camera);
}
animate();

// --- 2. دالة رسم الكائنات ---
function renderEntities(data, parentGroup = scene) {
    data.forEach(entity => {
        let mesh;
        let color = new THREE.Color(0xCCCCCC);
        const ltName = entity.dwgProperties.LineType || 'Continuous';

        if (entity.dwgProperties.Color) {
            const [r, g, b] = entity.dwgProperties.Color.split(',').map(c => parseInt(c.trim()) / 255.0);
            color = new THREE.Color(r, g, b);
        }

        const material = getLinetypeMaterial(ltName, color);

        if (entity.type === 'Line' && entity.geometry) {
            const points = [
                new THREE.Vector3(entity.geometry.startPoint[0], entity.geometry.startPoint[1], entity.geometry.startPoint[2] || 0),
                new THREE.Vector3(entity.geometry.endPoint[0], entity.geometry.endPoint[1], entity.geometry.endPoint[2] || 0)
            ];
            const geometry = new THREE.BufferGeometry().setFromPoints(points);
            mesh = new THREE.Line(geometry, material);
        }
        else if (entity.type === 'MLine' && entity.geometry) {
            const points = entity.geometry.vertices.map(p => new THREE.Vector3(p[0], p[1], p[2] || 0));
            if (points.length >= 2) {
                const geometry = new THREE.BufferGeometry().setFromPoints(points);
                mesh = new THREE.Line(geometry, material);
            }
        }
        else if (entity.type === 'Circle' && entity.geometry) {
            const center = entity.geometry.center;
            const radius = entity.geometry.radius;
            const curve = new THREE.EllipseCurve(0, 0, radius, radius, 0, 2 * Math.PI, false, 0);
            const points = curve.getPoints(128);
            const geometry = new THREE.BufferGeometry().setFromPoints(points);
            mesh = new THREE.LineLoop(geometry, material);
            mesh.position.set(center[0], center[1], center[2] || 0);
        }
        else if (entity.type === 'Arc' && entity.geometry) {
            const center = entity.geometry.center;
            const radius = entity.geometry.radius;
            let startAngle = entity.geometry.startAngle;
            let endAngle = entity.geometry.endAngle;
            const normal = entity.geometry.normal || [0, 0, 1];

            // Ensure angles are in valid range [0, 2π]
            // Normalize to [0, 2π)
            startAngle = ((startAngle % (2 * Math.PI)) + 2 * Math.PI) % (2 * Math.PI);
            endAngle = ((endAngle % (2 * Math.PI)) + 2 * Math.PI) % (2 * Math.PI);

            // Calculate arc angle
            let arcAngle = endAngle - startAngle;
            if (arcAngle <= 0) {
                arcAngle += 2 * Math.PI;
            }

            // Safety check: ensure we're not creating a full circle (arc angle should be < 2π)
            if (arcAngle >= 2 * Math.PI - 0.01) {
                // This is essentially a full circle, handle as circle
                const curve = new THREE.EllipseCurve(0, 0, radius, radius, 0, 2 * Math.PI, false, 0);
                const points = curve.getPoints(128);
                const geometry = new THREE.BufferGeometry().setFromPoints(points);
                mesh = new THREE.LineLoop(geometry, material);
            } else {
                // Create arc with proper start and end angles
                const curve = new THREE.EllipseCurve(0, 0, radius, radius, startAngle, endAngle, false, 0);
                const points = curve.getPoints(64);
                const geometry = new THREE.BufferGeometry().setFromPoints(points);
                mesh = new THREE.Line(geometry, material);
            }

            // Set position
            mesh.position.set(center[0], center[1], center[2] || 0);

            // Align to Normal (OCS)
            const targetNormal = new THREE.Vector3(normal[0], normal[1], normal[2]).normalize();
            const defaultNormal = new THREE.Vector3(0, 0, 1);
            if (Math.abs(targetNormal.z - 1) > 0.001) {
                // If not standard Z normal, rotate
                mesh.quaternion.setFromUnitVectors(defaultNormal, targetNormal);
            }
        }
        else if ((entity.type === 'LwPolyline' || entity.type === 'Polyline') && entity.geometry) {
            const points = entity.geometry.vertices.map(p => new THREE.Vector3(p[0], p[1], p[2] || 0));
            const geometry = new THREE.BufferGeometry().setFromPoints(points);
            if (entity.geometry.isClosed) {
                mesh = new THREE.LineLoop(geometry, material);
            } else {
                mesh = new THREE.Line(geometry, material);
            }
        }
        else if (entity.type === 'Spline' && entity.geometry) {
            const points = entity.geometry.controlPoints.map(p => new THREE.Vector3(p[0], p[1], p[2] || 0));
            if (points.length >= 2) {
                const curve = new THREE.CatmullRomCurve3(points);
                const geometry = new THREE.BufferGeometry().setFromPoints(curve.getPoints(50));
                mesh = new THREE.Line(geometry, material);
            }
        }
        else if (entity.type === 'Ellipse' && entity.geometry) {
            const center = entity.geometry.center;
            const majorAxis = entity.geometry.majorAxis;
            const minorAxisRatio = entity.geometry.minorAxisRatio;
            const startAngle = entity.geometry.startAngle;
            const endAngle = entity.geometry.endAngle;

            const majorAxisVector = new THREE.Vector3(majorAxis[0], majorAxis[1], majorAxis[2] || 0);
            const majorRadius = majorAxisVector.length();
            const rotation = Math.atan2(majorAxisVector.y, majorAxisVector.x);

            const curve = new THREE.EllipseCurve(
                0, 0, // x, y center (relative to mesh position)
                majorRadius, majorRadius * minorAxisRatio, // xRadius, yRadius
                startAngle, endAngle, // startAngle, endAngle
                false, // clockwise
                rotation // rotation
            );
            const points = curve.getPoints(128);
            const geometry = new THREE.BufferGeometry().setFromPoints(points);
            mesh = new THREE.Line(geometry, material);
            mesh.position.set(center[0], center[1], center[2] || 0);
        }
        else if (entity.type === 'Point' && entity.geometry) {
            const pos = entity.geometry.location;
            const geometry = new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(0, 0, 0)]);
            mesh = new THREE.Points(geometry, new THREE.PointsMaterial({ color: color, size: 5, sizeAttenuation: false }));
            mesh.position.set(pos[0], pos[1], pos[2] || 0);
        }
        else if (entity.type === 'Solid' && entity.geometry) {
            const points = entity.geometry.vertices.map(p => new THREE.Vector2(p[0], p[1]));
            if (points.length >= 3) {
                const shape = new THREE.Shape(points);
                const geometry = new THREE.ShapeGeometry(shape);
                const solidMaterial = new THREE.MeshBasicMaterial({ color: color, side: THREE.DoubleSide });
                mesh = new THREE.Mesh(geometry, solidMaterial);
                // Solids are typically 2D, so Z is often 0 or ignored.
                // If Z is present in vertices, it's handled by the ShapeGeometry implicitly if all Zs are the same.
                // For varying Z, a more complex extrusion or custom geometry would be needed.
            }
        }
        else if (entity.type === 'Face3D' && entity.geometry) {
            const vertices = entity.geometry.vertices;
            if (vertices && vertices.length >= 3) {
                const geometry = new THREE.BufferGeometry();
                const positions = [];
                for (let i = 0; i < vertices.length; i++) {
                    positions.push(vertices[i][0], vertices[i][1], vertices[i][2] || 0);
                }
                geometry.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));

                // Assuming faces are triangles or quads.
                // For quads, we can split into two triangles.
                const indices = [];
                if (vertices.length === 3) {
                    indices.push(0, 1, 2);
                } else if (vertices.length === 4) {
                    indices.push(0, 1, 2, 0, 2, 3); // Two triangles for a quad
                }
                geometry.setIndex(indices);
                geometry.computeVertexNormals(); // For proper lighting if using MeshStandardMaterial

                const faceMaterial = new THREE.MeshBasicMaterial({ color: color, side: THREE.DoubleSide });
                mesh = new THREE.Mesh(geometry, faceMaterial);
            }
        }
        else if ((entity.type === 'Text' || entity.type === 'MText') && entity.geometry) {
            mesh = createTextHelper(entity.geometry.text, entity.geometry.height, color);
            const pos = entity.geometry.insertionPoint;
            mesh.position.set(pos[0], pos[1], pos[2] || 0);
            mesh.rotation.z = entity.geometry.rotation || 0;
        }
        else if (entity.type === 'Insert' && entity.geometry) {
            mesh = new THREE.Group();
            const pos = entity.geometry.insertionPoint;
            const origin = entity.geometry.origin || [0, 0, 0];
            const sc = entity.geometry.scale;

            if (origin[0] !== 0 || origin[1] !== 0) {
                console.log(`Block Insert [${entity.geometry.blockName}] has origin offset:`, origin);
            }

            mesh.position.set(pos[0], pos[1], pos[2] || 0);
            mesh.scale.set(sc[0], sc[1], sc[2] || 1);
            mesh.rotation.z = entity.geometry.rotation || 0;

            // Create a content group to handle the block's internal origin (base point)
            const contentGroup = new THREE.Group();
            contentGroup.position.set(-origin[0], -origin[1], -origin[2] || 0);
            mesh.add(contentGroup);

            if (entity.entities && entity.entities.length > 0) {
                renderEntities(entity.entities, contentGroup);
            }
        }
        else if (entity.type.startsWith('Dimension') && entity.geometry) {
            mesh = new THREE.Group();
            if (entity.entities && entity.entities.length > 0) {
                renderEntities(entity.entities, mesh);
            }
        }
        else if (entity.type === 'Viewport' && entity.geometry) {
            const geo = entity.geometry;
            const width = geo.width;
            const height = geo.height;
            const center = geo.center;

            // Viewports are rectangles on Layer 0 or Defpoints
            const shape = new THREE.Shape();
            shape.moveTo(-width / 2, -height / 2);
            shape.lineTo(width / 2, -height / 2);
            shape.lineTo(width / 2, height / 2);
            shape.lineTo(-width / 2, height / 2);
            shape.lineTo(-width / 2, -height / 2);

            const geometry = new THREE.BufferGeometry().setFromPoints(shape.getPoints());
            mesh = new THREE.LineLoop(geometry, new THREE.LineBasicMaterial({ color: 0xFFFFFF, opacity: 0.5, transparent: true }));
            mesh.position.set(center[0], center[1], center[2] || 0);
        }
        else if (entity.type === 'Leader' && entity.geometry) {
            const points = entity.geometry.vertices.map(p => new THREE.Vector3(p[0], p[1], p[2] || 0));
            if (points.length >= 2) {
                const geometry = new THREE.BufferGeometry().setFromPoints(points);
                mesh = new THREE.Line(geometry, material);
            }
        }
        else if (entity.type === 'Hatch' && entity.geometry) {
            mesh = new THREE.Group();
            if (entity.geometry.boundaries) {
                entity.geometry.boundaries.forEach(path => {
                    const points = path.map(p => new THREE.Vector3(p[0], p[1], p[2] || 0));
                    if (points.length > 0) {
                        const geom = new THREE.BufferGeometry().setFromPoints(points);
                        const boundaryMesh = new THREE.LineLoop(geom, new THREE.LineBasicMaterial({
                            color: color,
                            transparent: true,
                            opacity: 0.5
                        }));
                        mesh.add(boundaryMesh);
                    }
                });
            }

        }

        if (mesh) {
            mesh.userData = entity.dwgProperties;
            mesh.userData.entityId = entity.id;
            mesh.userData.layer = entity.dwgProperties.Layer;

            if (mesh.material instanceof THREE.LineDashedMaterial) {
                mesh.computeLineDistances();
            }

            parentGroup.add(mesh);

            // Only add top-level objects to interact with, or specific interactive types
            // If it's a child of a scene, it's a top-level entity.
            // If it's a child of a Group (Insert), we want to select the Group instead.
            if (parentGroup === scene) {
                objectsToIntersect.push(mesh);
            }
        }
    });

    if (parentGroup === scene && objectsToIntersect.length > 0) {
        const box = new THREE.Box3().setFromObject(scene);
        const size = box.getSize(new THREE.Vector3());
        const center = box.getCenter(new THREE.Vector3());
        camera.position.set(center.x, center.y, 100);
        camera.lookAt(center.x, center.y, 0);
        const maxDim = Math.max(size.x, size.y);
        zoom = frustumSize / (maxDim * 1.2);
        minZoom = zoom / 2;
        panOffset.x = 0;
        panOffset.y = 0;
        updateCamera();
    }
}

function getLinetypeMaterial(name, color) {
    const pattern = linetypes[name];
    if (!pattern || pattern.length === 0 || name.toLowerCase() === 'continuous') {
        return new THREE.LineBasicMaterial({ color: color, linewidth: 2 });
    }

    let dashSize = 1;
    let gapSize = 1;

    for (let i = 0; i < pattern.length; i++) {
        if (pattern[i] > 0) dashSize = pattern[i];
        if (pattern[i] < 0) gapSize = Math.abs(pattern[i]);
    }

    return new THREE.LineDashedMaterial({
        color: color,
        linewidth: 2,
        dashSize: dashSize,
        gapSize: gapSize,
        scale: 1
    });
}

function createLayersPanel(layers) {
    const layersSection = document.getElementById('layers-section');

    // Create collapsible header and content
    let html = `
        <div class="section-header" onclick="toggleSection(this)">
            <span>🗂️ الطبقات (${layers?.length || 0})</span>
        </div>
        <div class="section-content" id="layers-list">
    `;

    if (!layers || layers.length === 0) {
        html += '<p class="empty-msg">لا توجد طبقات متاحة.</p>';
    } else {
        layers.forEach(layer => {
            html += `
                <div class="layer-item">
                    <input type="checkbox" class="layer-checkbox" checked 
                        onchange="toggleLayerVisibility('${layer.name}', this.checked)">
                    <div class="layer-color-dot" style="background-color: rgb(${layer.color})"></div>
                    <span class="layer-name" title="${layer.name}">${layer.name}</span>
                </div>
            `;
        });
    }

    html += '</div>';
    layersSection.innerHTML = html;
}

function displayMetadata(metadata) {
    if (!metadata) return;

    const statsSection = document.getElementById('stats-section');
    const statsContent = document.getElementById('stats-content');
    statsSection.style.display = 'block';

    let html = `
        <div class="stats-grid">
            <div class="stat-card">
                <span class="stat-value">${metadata.TotalEntitiesFound || 0}</span>
                <span class="stat-label">إجمالي العناصر</span>
            </div>
            <div class="stat-card">
                <span class="stat-value">${metadata.TotalEntitiesConverted || 0}</span>
                <span class="stat-label">تم تحويله</span>
            </div>
        </div>
        <div class="stats-detail-list">
            <h4 style="color:#a0aec0; font-size:0.8em; margin-bottom:10px; border-bottom:1px solid rgba(255,255,255,0.1); padding-bottom:5px;">تفاصيل الأنواع (في الملف):</h4>
    `;

    if (metadata.DetailedStats_AllInFile) {
        for (const [type, count] of Object.entries(metadata.DetailedStats_AllInFile)) {
            const converted = metadata.DetailedStats_Converted?.[type] || 0;
            const isFullySupported = converted === count;

            html += `
                <div class="stats-detail-item">
                    <span class="stats-type-name">${type}</span>
                    <span class="stats-type-count" style="color: ${isFullySupported ? '#48bb78' : '#ed8936'}">
                        ${converted} / ${count} ${isFullySupported ? '✅' : '⚠️'}
                    </span>
                </div>
            `;
        }
    }

    html += '</div>';
    statsContent.innerHTML = html;
}

function toggleSection(header) {
    header.classList.toggle('collapsed');
    header.nextElementSibling.classList.toggle('collapsed');
}

function toggleLayerVisibility(layerName, isVisible) {
    scene.traverse(obj => {
        if (obj.userData && obj.userData.layer === layerName) {
            obj.visible = isVisible;
        }
    });
}

function createTextHelper(text, height, color) {
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');
    const fontSize = 64; // High resolution for texture
    ctx.font = `${fontSize}px Arial`;

    const metrics = ctx.measureText(text);
    const textWidth = metrics.width;
    const textHeight = fontSize * 1.2;

    canvas.width = textWidth;
    canvas.height = textHeight;

    // Redraw with correct size
    ctx.font = `${fontSize}px Arial`;
    ctx.fillStyle = `#${color.getHexString()}`;
    ctx.textBaseline = 'middle';
    ctx.fillText(text, 0, textHeight / 2);

    const texture = new THREE.CanvasTexture(canvas);
    texture.minFilter = THREE.LinearFilter;
    texture.magFilter = THREE.LinearFilter;

    const material = new THREE.MeshBasicMaterial({
        map: texture,
        transparent: true,
        side: THREE.DoubleSide
    });

    const aspectRatio = textWidth / textHeight;
    const planeHeight = height || 2;
    const planeWidth = planeHeight * aspectRatio;

    const geometry = new THREE.PlaneGeometry(planeWidth, planeHeight);
    const mesh = new THREE.Mesh(geometry, material);

    // Anchor to bottom-left (Standard CAD text behavior)
    mesh.geometry.translate(planeWidth / 2, 0, 0);

    return mesh;
}

// --- 3. دالة تصنيف الخصائص ---
function categorizeProperties(properties) {
    const categories = {
        'معلومات أساسية': ['ObjectType', 'ObjectName', 'Handle', 'OwnerHandle', 'Space'],
        'الطبقة والرسم': ['Layer', 'LayerHandle'],
        'اللون والمظهر': ['Color', 'ColorIndex', 'ColorMethod', 'IsByLayer', 'IsByBlock'],
        'الخط والسُمك': ['LineType', 'LineTypeHandle', 'LineTypeScale', 'LineWeight', 'Thickness'],
        'الشفافية والرؤية': ['Transparency', 'IsInvisible', 'ShadowMode'],
        'الإحداثيات والأبعاد': [],
        'خصائص هندسية': [],
        'بيانات ممتدة (XData)': [],
        'خصائص إضافية': []
    };

    for (const key in properties) {
        let categorized = false;

        for (const [category, keys] of Object.entries(categories)) {
            if (keys.includes(key)) {
                categorized = true;
                break;
            }
        }

        if (!categorized) {
            if (key.includes('Point') || key.includes('Center') || key.includes('Position')) {
                categories['الإحداثيات والأبعاد'].push(key);
            }
            else if (key.includes('Radius') || key.includes('Length') || key.includes('Area') ||
                key.includes('Angle') || key.includes('Width') || key.includes('Height') ||
                key.includes('Delta') || key.includes('Diameter')) {
                categories['خصائص هندسية'].push(key);
            }
            else if (key.startsWith('XData')) {
                categories['بيانات ممتدة (XData)'].push(key);
            }
            else if (key.startsWith('Reflection_')) {
                categories['خصائص إضافية'].push(key);
            }
            else {
                categories['خصائص إضافية'].push(key);
            }
        }
    }

    for (const category in categories) {
        if (categories[category].length === 0) {
            delete categories[category];
        }
    }

    return categories;
}

// --- 4. عرض الخصائص ---
function displayProperties(properties) {
    const propertiesPanel = document.getElementById('properties-panel');
    const categories = categorizeProperties(properties);

    let htmlContent = '<h3>📋 معلومات العنصر</h3>';

    for (const [categoryName, keys] of Object.entries(categories)) {
        htmlContent += `
                    <div class="property-category">
                        <div class="category-header" onclick="toggleCategory(this)">
                            <span>${getCategoryIcon(categoryName)} ${categoryName}</span>
                            <span class="category-toggle">▼</span>
                        </div>
                        <div class="category-content">
                `;

        keys.forEach(key => {
            const value = properties[key];
            let displayValue = formatPropertyValue(key, value);

            htmlContent += `
                        <div class="property-item">
                            <div class="property-key">${key}</div>
                            <div class="property-value">${displayValue}</div>
                        </div>
                    `;
        });

        htmlContent += `</div></div>`;
    }

    propertiesPanel.innerHTML = htmlContent;
}

function getCategoryIcon(categoryName) {
    const icons = {
        'معلومات أساسية': '🆔',
        'الطبقة والرسم': '📂',
        'اللون والمظهر': '🎨',
        'الخط والسُمك': '📏',
        'الشفافية والرؤية': '👁️',
        'الإحداثيات والأبعاد': '📍',
        'خصائص هندسية': '📐',
        'بيانات ممتدة (XData)': '🔖',
        'خصائص إضافية': '⚙️'
    };
    return icons[categoryName] || '📌';
}

function formatPropertyValue(key, value) {
    if (value === null || value === undefined) return 'غير محدد';

    if (key === 'Color') {
        const colorName = getRgbColorName(value);
        return `${colorName} <span class="color-swatch" style="background:rgb(${value});"></span>`;
    }

    if (typeof value === 'boolean') {
        return value ? '✅ نعم' : '❌ لا';
    }

    if (typeof value === 'number' && Math.abs(value) > 1000) {
        return value.toFixed(2);
    }

    return value.toString();
}

function getRgbColorName(rgbString) {
    if (!rgbString) return 'غير محدد';
    const [r, g, b] = rgbString.split(',').map(c => parseInt(c.trim()));
    const colorNames = {
        '255,0,0': 'أحمر', '0,255,0': 'أخضر', '0,0,255': 'أزرق',
        '255,255,0': 'أصفر', '255,0,255': 'وردي', '0,255,255': 'سماوي',
        '0,0,0': 'أسود', '255,255,255': 'أبيض', '128,128,128': 'رمادي'
    };
    return colorNames[`${r},${g},${b}`] || `RGB(${r}, ${g}, ${b})`;
}

function toggleCategory(header) {
    header.classList.toggle('collapsed');
    const content = header.nextElementSibling;
    content.classList.toggle('collapsed');
}

// --- 5. البحث بالـ ID ---
const searchIdBtn = document.getElementById('search-id-btn');
const searchIdInput = document.getElementById('search-id-input');
const searchResult = document.getElementById('search-result');

searchIdBtn.addEventListener('click', () => {
    const searchId = searchIdInput.value.trim().toUpperCase();
    if (searchId) {
        searchById(searchId);
    }
});

searchIdInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
        const searchId = searchIdInput.value.trim().toUpperCase();
        if (searchId) {
            searchById(searchId);
        }
    }
});

function searchById(handleId) {
    // البحث في كل الكائنات
    const foundMesh = objectsToIntersect.find(obj =>
        obj.userData.Handle && obj.userData.Handle.toUpperCase() === handleId
    );

    if (foundMesh) {
        // تحديد الكائن
        highlightObject(foundMesh);
        displayProperties(foundMesh.userData);

        // عرض رسالة نجاح
        showSearchResult(`✅ تم العثور على العنصر بـ Handle: ${handleId}`, false);
    } else {
        // عرض رسالة فشل
        showSearchResult(`❌ لم يتم العثور على عنصر بـ Handle: ${handleId}`, true);
    }
}

function showSearchResult(message, isError) {
    searchResult.textContent = message;
    searchResult.className = isError ? 'error' : '';
    searchResult.style.display = 'block';

    setTimeout(() => {
        searchResult.style.display = 'none';
    }, 3000);
}

// --- 6. التحديد والتفاعل ---
function highlightObject(object) {
    // Reset previous selection
    if (selectedMesh) {
        selectedMesh.traverse(child => {
            if (child.isMesh || child.isLine || child.isPoints) {
                if (child.originalColor !== undefined) {
                    child.material.color.setHex(child.originalColor);
                    child.material.linewidth = child.originalLinewidth || 1;
                }
            }
        });
    }

    // Highlight new selection
    if (object) {
        object.traverse(child => {
            if (child.isMesh || child.isLine || child.isPoints) {
                if (child.originalColor === undefined) {
                    child.originalColor = child.material.color.getHex();
                    child.originalLinewidth = child.material.linewidth || 1;
                }
                child.material.color.set(0xFF0000); // Bright Red
                child.material.linewidth = 5;
            }
        });
    }
    selectedMesh = object;
}

function onMouseClick(event) {
    // Only process left-click (button 0)
    if (event.button !== 0) return;

    const rect = renderer.domElement.getBoundingClientRect();
    pointer.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
    pointer.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

    // raycaster.params.Line.threshold = 5 / zoom; // This line was removed in the original diff, assuming it's no longer needed or handled differently.
    raycaster.setFromCamera(pointer, camera);
    const intersects = raycaster.intersectObjects(objectsToIntersect, true); // 'true' for recursive intersection

    if (intersects.length > 0) {
        let object = intersects[0].object;

        // Traverse up to find the top-level selectable object (like an Insert/Group)
        // This assumes objectsToIntersect contains the top-level selectable groups/meshes.
        while (object.parent && object.parent !== scene && objectsToIntersect.indexOf(object) === -1) {
            object = object.parent;
        }

        highlightObject(object);
        displayProperties(object.userData);
    } else {
        highlightObject(null);
        document.getElementById('properties-panel').innerHTML =
            '<h3>📋 خصائص الكائن</h3><p>انقر على أحد عناصر الرسم أو ابحث بالـ Handle.</p>';
    }
}

// --- 7. رفع الملفات ---
const uploadBtn = document.getElementById('upload-btn');
const fileInput = document.getElementById('dwg-file-input');
const fileNameSpan = document.getElementById('file-name');

uploadBtn.addEventListener('click', () => fileInput.click());

fileInput.addEventListener('change', (event) => {
    const file = event.target.files[0];
    if (file) {
        fileNameSpan.textContent = `📄 ${file.name} `;
        uploadDwgFile(file);
    }
});

function uploadDwgFile(file) {
    const formData = new FormData();
    formData.append('file', file);
    document.getElementById('properties-panel').innerHTML = '<h3> جاري تحميل الملف...</h3>';

    fetch('http://localhost:5183/api/dwg/upload', {
        method: 'POST',
        body: formData
    })
        .then(response => {
            if (!response.ok) throw new Error('الملف يجب أن يكون .DWG');
            return response.json();
        })
        .then(data => {
            objectsToIntersect.forEach(obj => scene.remove(obj));
            objectsToIntersect.length = 0;
            storeLinetypes(data.linetypes);
            renderEntities(data.entities);
            createLayersPanel(data.layers);
            displayMetadata(data.metadata);
            document.getElementById('properties-panel').innerHTML =
                '<h3>✅ تم التحميل بنجاح</h3><p>انقر على أحد عناصر الرسم.</p>';
        })
        .catch(error => {
            console.error('Error:', error);
            document.getElementById('properties-panel').innerHTML =
                `< h3 >❌ خطأ</h3 > <p>${error.message}</p>`;
        });
}

// --- 8. التحميل من رابط ---
const loadUrlBtn = document.getElementById('load-url-btn');
const urlInput = document.getElementById('dwg-url-input');

loadUrlBtn.addEventListener('click', () => {
    const url = urlInput.value.trim();
    if (url) loadDwgFromUrl(url);
    else alert('يرجى إدخال رابط صحيح.');
});

function loadDwgFromUrl(dwgUrl) {
    document.getElementById('properties-panel').innerHTML = '<h3> جاري التحميل من الرابط...</h3>';

    fetch('http://localhost:5183/api/dwg/parse-from-url', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ url: dwgUrl })
    })
        .then(response => {
            if (!response.ok) throw new Error('فشل تحميل الملف من الرابط.');
            return response.json();
        })
        .then(data => {
            objectsToIntersect.forEach(obj => scene.remove(obj));
            objectsToIntersect.length = 0;
            storeLinetypes(data.linetypes);
            renderEntities(data.entities);
            createLayersPanel(data.layers);
            displayMetadata(data.metadata);
            document.getElementById('properties-panel').innerHTML =
                '<h3>✅ تم التحميل بنجاح</h3><p>انقر على أحد عناصر الرسم.</p>';
            fileNameSpan.textContent = `🌐 ملف من رابط`;
        })
        .catch(error => {
            console.error('Error:', error);
            document.getElementById('properties-panel').innerHTML =
                `< h3 >❌ خطأ</h3 > <p>${error.message}</p>`;
        });
}

// --- 9. Zoom و Pan ---
function updateCamera() {
    const size = frustumSize / zoom;
    camera.left = (-size * aspect / 2) + panOffset.x;
    camera.right = (size * aspect / 2) + panOffset.x;
    camera.top = (size / 2) + panOffset.y;
    camera.bottom = (-size / 2) + panOffset.y;
    camera.updateProjectionMatrix();
}

container.addEventListener('wheel', (event) => {
    event.preventDefault();
    const zoomSpeed = 0.95;
    zoom = event.deltaY < 0 ? zoom / zoomSpeed : zoom * zoomSpeed;
    zoom = Math.max(minZoom, Math.min(zoom, 50));
    updateCamera();
});

function storeLinetypes(data) {
    linetypes = {};
    if (!data) return;
    data.forEach(lt => {
        linetypes[lt.name] = lt.pattern;
    });
}

container.addEventListener('mousedown', (event) => {
    if (event.button === 2 || event.button === 1) {
        event.preventDefault();
        isPanning = true;
        panStart.x = event.clientX;
        panStart.y = event.clientY;
        container.style.cursor = 'grabbing';
    }
});

container.addEventListener('mousemove', (event) => {
    if (isPanning) {
        const deltaX = (event.clientX - panStart.x) * (frustumSize / zoom / width);
        const deltaY = -(event.clientY - panStart.y) * (frustumSize / zoom / height);
        panOffset.x -= deltaX;
        panOffset.y -= deltaY;
        updateCamera();
        panStart.x = event.clientX;
        panStart.y = event.clientY;
    }
});

container.addEventListener('mouseup', () => {
    if (isPanning) {
        isPanning = false;
        container.style.cursor = 'default';
    }
});

container.addEventListener('contextmenu', (e) => e.preventDefault());
container.addEventListener('click', onMouseClick);

window.addEventListener('resize', () => {
    width = container.clientWidth;
    height = container.clientHeight;
    aspect = width / height;
    renderer.setSize(width, height);
    updateCamera();
});