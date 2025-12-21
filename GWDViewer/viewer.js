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
            const points = entity.geometry.points.map(p => new THREE.Vector3(p[0], p[1], p[2] || 0));
            const geometry = new THREE.BufferGeometry().setFromPoints(points);
            mesh = new THREE.Line(geometry, material);
        }
        else if (entity.type === 'MLine' && entity.geometry) {
            // MLine: draw as connected line segments
            const points = entity.geometry.points.map(p => new THREE.Vector3(p[0], p[1], p[2] || 0));
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
            const startAngle = entity.geometry.startAngle;
            const endAngle = entity.geometry.endAngle;
            const curve = new THREE.EllipseCurve(0, 0, radius, radius, startAngle, endAngle, false, 0);
            const points = curve.getPoints(64);
            const geometry = new THREE.BufferGeometry().setFromPoints(points);
            mesh = new THREE.Line(geometry, material);
            mesh.position.set(center[0], center[1], center[2] || 0);
        }
        else if (entity.type === 'LwPolyline' && entity.geometry) {
            const points = entity.geometry.points.map(p => new THREE.Vector3(p[0], p[1], p[2] || 0));
            const geometry = new THREE.BufferGeometry().setFromPoints(points);

            if (entity.geometry.isClosed) {
                mesh = new THREE.LineLoop(geometry, material);
            } else {
                mesh = new THREE.Line(geometry, material);
            }
        }
        else if ((entity.type === 'Text' || entity.type === 'MText') && entity.geometry) {
            mesh = createTextHelper(entity.geometry.content, entity.geometry.height, color);
            const pos = entity.geometry.position;
            mesh.position.set(pos[0], pos[1], pos[2] || 0);
            mesh.rotation.z = entity.geometry.rotation || 0;
        }
        else if (entity.type === 'Insert' && entity.geometry) {
            mesh = new THREE.Group();
            const pos = entity.geometry.position;
            const sc = entity.geometry.scale;
            mesh.position.set(pos[0], pos[1], pos[2] || 0);
            mesh.scale.set(sc[0], sc[1], sc[2] || 1);
            mesh.rotation.z = entity.geometry.rotation || 0;

            // Draw entities inside the block recursively
            if (entity.entities && entity.entities.length > 0) {
                renderEntities(entity.entities, mesh);
            }
        }
        else if (entity.type === 'Hatch' && entity.geometry) {
            mesh = new THREE.Group();
            console.log(`Hatch ${entity.id}: Segpoints=${entity.geometry.patternLines?.length}, Paths=${entity.geometry.paths?.length}`);

            // 1. Draw boundaries
            if (entity.geometry.paths) {
                entity.geometry.paths.forEach(path => {
                    const points = path.map(p => new THREE.Vector3(p[0], p[1], p[2] || 0));
                    if (points.length > 0) {
                        const geom = new THREE.BufferGeometry().setFromPoints(points);
                        const boundaryMesh = new THREE.LineLoop(geom, new THREE.LineBasicMaterial({
                            color: 0xFF0000,
                            transparent: true,
                            opacity: 0.5
                        }));
                        mesh.add(boundaryMesh);
                    }
                });
            }

            // 2. Draw pattern lines (Exploded lines)
            if (entity.geometry.patternLines && entity.geometry.patternLines.length > 0) {
                const segPoints = [];
                for (let i = 0; i < entity.geometry.patternLines.length; i++) {
                    const p = entity.geometry.patternLines[i];
                    segPoints.push(new THREE.Vector3(p[0], p[1], p[2] || 0));
                }
                const geom = new THREE.BufferGeometry().setFromPoints(segPoints);
                const patternMesh = new THREE.LineSegments(geom, new THREE.LineBasicMaterial({ color: 0x00FFFF }));
                mesh.add(patternMesh);
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
            if (mesh instanceof THREE.Line || mesh instanceof THREE.Mesh || mesh instanceof THREE.LineLoop || mesh instanceof THREE.Group) {
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

    // Three.js LineDashedMaterial only supports simple patterns (dashSize, gapSize)
    // We'll take the first non-zero dash and the first non-zero gap
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
        <div class="section-header" onclick="toggleLayersSection(this)">
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

function toggleLayersSection(header) {
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

    let htmlContent = '<h3>📋 معلومات الكائن</h3>';

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
function highlightObject(mesh) {
    // إعادة العنصر السابق لحالته الأصلية
    if (selectedMesh) {
        selectedMesh.material.color.setHex(selectedMesh.originalColor);
        selectedMesh.material.linewidth = selectedMesh.originalLinewidth || 1;
    }

    // تحديد العنصر الجديد بتأثيرات مميزة
    if (mesh) {
        mesh.originalColor = mesh.material.color.getHex();
        mesh.originalLinewidth = mesh.material.linewidth || 1;

        mesh.material.color.set(0xFF0000); // أحمر فاقع
        mesh.material.linewidth = 5; // خط أتخن
        mesh.material.needsUpdate = true;
    }
    selectedMesh = mesh;
}

function onMouseClick(event) {
    const rect = renderer.domElement.getBoundingClientRect();
    pointer.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
    pointer.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

    raycaster.params.Line.threshold = 5 / zoom;
    raycaster.setFromCamera(pointer, camera);
    const intersects = raycaster.intersectObjects(objectsToIntersect);

    if (intersects.length > 0) {
        const selectedObject = intersects[0].object;
        highlightObject(selectedObject);
        displayProperties(selectedObject.userData);
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
        fileNameSpan.textContent = `📄 ${file.name}`;
        uploadDwgFile(file);
    }
});

function uploadDwgFile(file) {
    const formData = new FormData();
    formData.append('file', file);
    document.getElementById('properties-panel').innerHTML = '<h3>⏳ جاري تحميل الملف...</h3>';

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
            document.getElementById('properties-panel').innerHTML =
                '<h3>✅ تم التحميل بنجاح</h3><p>انقر على أحد عناصر الرسم.</p>';
        })
        .catch(error => {
            console.error('Error:', error);
            document.getElementById('properties-panel').innerHTML =
                `<h3>❌ خطأ</h3><p>${error.message}</p>`;
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
            document.getElementById('properties-panel').innerHTML =
                '<h3>✅ تم التحميل بنجاح</h3><p>انقر على أحد عناصر الرسم.</p>';
            fileNameSpan.textContent = `🌐 ملف من رابط`;
        })
        .catch(error => {
            console.error('Error:', error);
            document.getElementById('properties-panel').innerHTML =
                `<h3>❌ خطأ</h3><p>${error.message}</p>`;
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