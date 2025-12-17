// --- 1. الإعداد الأساسي للمشهد ---
const container = document.getElementById('viewer');
const scene = new THREE.Scene();
let width = container.clientWidth;
let height = container.clientHeight;
let aspect = width / height;
const frustumSize = 100; // حجم المشهد الأولي

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

const objectsToIntersect = []; // قائمة بالكائنات التي يمكن النقر عليها
const raycaster = new THREE.Raycaster();
const pointer = new THREE.Vector2();
let selectedMesh = null; // الكائن المحدد حالياً

// --- 2. دالة الرسوم المتحركة ---
function animate() {
    requestAnimationFrame(animate);
    renderer.render(scene, camera);
}
animate();

// --- 3. دالة رسم الكائنات من JSON ---
function renderEntities(data) {
    data.forEach(entity => {
        let mesh;
        let color = new THREE.Color(0xCCCCCC); // لون افتراضي

        // محاولة تحليل لون الـ DWG (RGB)
        if (entity.dwgProperties.Color) {
            // [r, g, b] يجب أن تكون أرقام بين 0 و 255
            const [r, g, b] = entity.dwgProperties.Color.split(',').map(c => parseInt(c.trim()) / 255.0);
            color = new THREE.Color(r, g, b);
        }

        if (entity.type === 'Line') {
            const points = entity.geometry.points.map(p => new THREE.Vector3(p[0], p[1], p[2] || 0));
            const geometry = new THREE.BufferGeometry().setFromPoints(points);
            const material = new THREE.LineBasicMaterial({ color: color, linewidth: 2 });
            mesh = new THREE.Line(geometry, material);
        }

        else if (entity.type === 'Circle') {
            const center = entity.geometry.center;
            const radius = entity.geometry.radius;

            // استخدام EllipseCurve للحصول على نقاط المحيط (لأنها أكثر دقة من CircleGeometry في 2D CAD View)
            const curve = new THREE.EllipseCurve(
                0, 0, // ax, aY - المركز مؤقتاً عند الصفر
                radius, radius, // xRadius, yRadius
                0, 2 * Math.PI, // aStartAngle, aEndAngle
                false,// aClockwise
                0 // aRotation
            );

            const points = curve.getPoints(64); // الحصول على 64 نقطة لتمثيل المحيط
            const geometry = new THREE.BufferGeometry().setFromPoints(points);
            const material = new THREE.LineBasicMaterial({ color: color });

            mesh = new THREE.LineLoop(geometry, material);
            // ضبط موقع Mesh النهائي بناءً على مركز الدائرة في DWG
            mesh.position.set(center[0], center[1], center[2] || 0);
        }

        else if (entity.type === 'Text') {
            // ملاحظة: Mesh غير مرئي لكن قابل للنقر (Selectable) لقراءة الخصائص
            const position = entity.geometry.position;
            const textGeometry = new THREE.SphereGeometry(2, 8, 8); // حجم صغير
            const material = new THREE.MeshBasicMaterial({ visible: false });

            mesh = new THREE.Mesh(textGeometry, material);
            mesh.position.set(position[0], position[1], position[2] || 0);

            // *لإظهار النص فعلياً، يمكنك استخدام عناصر HTML/CSS فوق الـ Viewer (DOM Labels)*
        }

        // *** نقطة التخزين والتفاعل ***
        if (mesh) {
            // تخزين الخصائص العميقة بالكامل لقراءة بيانات DWG المخصصة (XData)
            mesh.userData = entity.dwgProperties;
            scene.add(mesh);
            objectsToIntersect.push(mesh);
        }
    });

    // (منطق ضبط الكاميرا - Zoom to Extents)
    if (objectsToIntersect.length > 0) {
        const box = new THREE.Box3().setFromObject(scene);
        const size = box.getSize(new THREE.Vector3());
        const center = box.getCenter(new THREE.Vector3());
        camera.position.set(center.x, center.y, 100);
        // camera.lookAt(center);
        camera.lookAt(center.x, center.y, 0);

        // const maxDim = Math.max(size.x, size.y) * 1.2;
        const maxDim = Math.max(size.x, size.y);
        // zoom = frustumSize / maxDim;
        zoom = frustumSize / (maxDim * 1.2);
        minZoom = zoom / 2;
        panOffset.x = 0;
        panOffset.y = 0;
        updateCamera();
    }
}

// دالة محاكاة التحميل (استبدل هذا بـ fetch(API_ENDPOINT) لاحقاً)
// ... (الكود السابق لتهيئة Three.js) ...

// دالة تحميل البيانات من ملف JSON
function loadViewer(jsonUrl) {
    // استخدام fetch لجلب الملف
    fetch(jsonUrl)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok: ' + response.statusText);
            }
            return response.json(); // تحويل الاستجابة إلى كائن JSON
        })
        .then(data => {
            // بمجرد الحصول على البيانات، ابدأ عملية الرسم
            renderEntities(data);
        })
        .catch(error => {
            console.error('There has been a problem fetching the data:', error);
            alert('Could not load DWG data. Check if dwg_data.json exists.');
        });
}

// استدعاء الدالة لتحميل ملف البيانات
// loadViewer('dwg_data.json'); // <-- قم بتغيير اسم الملف هنا

// loadViewerFromAPI('E:\\AutoCadViewr\\DWG Sample\\architectural_-_annotation_scaling_and_multileaders.dwg');

// --- 5. معالجة رفع الملفات ---
const uploadBtn = document.getElementById('upload-btn');
const fileInput = document.getElementById('dwg-file-input');
const fileNameSpan = document.getElementById('file-name');

uploadBtn.addEventListener('click', () => {
    fileInput.click();
});

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

    // عرض رسالة تحميل
    const propertiesPanel = document.getElementById('properties-panel');
    propertiesPanel.innerHTML = '<h3>⏳ جاري تحميل الملف...</h3>';

    fetch('http://localhost:5183/api/dwg/upload', {
        method: 'POST',
        body: formData
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(' {.DWG} الملف يجب ان يكون ');
            }
            return response.json();
        })
        .then(data => {
            // مسح الكائنات القديمة
            objectsToIntersect.forEach(obj => scene.remove(obj));
            objectsToIntersect.length = 0;

            // رسم البيانات الجديدة
            renderEntities(data);
            propertiesPanel.innerHTML = '<h3>✅ تم التحميل بنجاح</h3><p>انقر على أحد عناصر الرسم.</p>';
        })
        .catch(error => {
            console.error('Error uploading file:', error);
            propertiesPanel.innerHTML = `<h3>❌ خطأ</h3><p>${error.message}</p>`;
        });
} function loadViewerFromAPI(dwgFilePath) {
    const apiUrl = `http://localhost:5183/api/dwg/parse?filePath=${encodeURIComponent(dwgFilePath)}`;

    fetch(apiUrl)
        .then(response => {
            if (!response.ok) {
                throw new Error('API Error: ' + response.statusText);
            }
            return response.json();
        })
        .then(data => {
            renderEntities(data);
        })
        .catch(error => {
            console.error('Error loading from API:', error);
            alert('Could not load DWG from API. Check console.');
        });
}

// دالة لتسليط الضوء على الكائن المحدد وإعادة اللون للكائن السابق
function highlightObject(mesh) {
    if (selectedMesh) {
        // إذا كان هناك كائن محدد سابقاً، أعد لونه الأصلي
        selectedMesh.material.color.setHex(selectedMesh.originalColor);
    }

    if (mesh) {
        // تسليط الضوء على الكائن الجديد
        mesh.originalColor = mesh.material.color.getHex(); // تخزين اللون الأصلي
        mesh.material.color.set(0xFFFF00); // تغيير اللون إلى الأصفر
    }
    selectedMesh = mesh;
}
// دالة لتحويل RGB إلى اسم اللون
function getRgbColorName(rgbString) {
    if (!rgbString) return 'غير محدد';

    const [r, g, b] = rgbString.split(',').map(c => parseInt(c.trim()));

    // قاموس الألوان الشائعة
    const colorNames = {
        '255,0,0': 'أحمر',
        '0,255,0': 'أخضر',
        '0,0,255': 'أزرق',
        '255,255,0': 'أصفر',
        '255,0,255': 'وردي',
        '0,255,255': 'سماوي',
        '0,0,0': 'أسود',
        '255,255,255': 'أبيض',
        '128,128,128': 'رمادي',
        '255,165,0': 'برتقالي',
        '128,0,128': 'بنفسجي',
        '165,42,42': 'بني'
    };

    const key = `${r},${g},${b}`;
    return colorNames[key] || `RGB(${r}, ${g}, ${b})`;
}
// دالة التعامل مع النقر
function onMouseClick(event) {
    const rect = renderer.domElement.getBoundingClientRect();
    pointer.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
    pointer.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

    console.log('🖱️ Click detected at:', pointer);
    console.log('📦 Total objects to check:', objectsToIntersect.length);

    raycaster.params.Line.threshold = 5 / zoom;
    raycaster.setFromCamera(pointer, camera);
    const intersects = raycaster.intersectObjects(objectsToIntersect);
    console.log('📦 Intersected objects:', intersects.length);
    const propertiesPanel = document.getElementById('properties-panel');

    if (intersects.length > 0) {
        const selectedObject = intersects[0].object;
        console.log('✅ Selected object:', selectedObject);
        console.log('📋 Properties:', selectedObject.userData);

        // 2. تفعيل التحديد وتغيير اللون
        highlightObject(selectedObject);

        // 3. قراءة الخصائص المخزنة بالكامل
        const properties = selectedObject.userData;

        let htmlContent = `<h3>Eleement Info</h3>`;
        htmlContent += `<div class="property-item">
    <div class="property-key">ID</div>
    <div class="property-value">${properties.Handle}</div>
</div>`;
        // عرض باقي الخصائص
        for (const key in properties) {
            if (key !== 'Handle') {
                // معالجة خاصة للون
                let displayValue = properties[key];
                if (key === 'Color') {
                    const colorName = getRgbColorName(properties[key]);
                    displayValue = `${colorName} <span style="display:inline-block; width:20px; height:20px; background:rgb(${properties[key]}); border:1px solid white; border-radius:3px; vertical-align:middle; margin-left:5px;"></span>`;
                }
                htmlContent += `<div class="property-item">
            <div class="property-key">${key}</div>
            <div class="property-value">${displayValue}</div>
        </div>`;
            }
        }
        propertiesPanel.innerHTML = htmlContent;
    } else {
        // إلغاء التحديد
        highlightObject(null);
        propertiesPanel.innerHTML = "<h3>خصائص الكائن المحدد</h3><p>انقر على أحد عناصر الرسم.</p>";
    }
}
// --- 4. إضافة Zoom و Pan ---

// متغيرات للتحكم في الكاميرا
let zoom = 1;
let isPanning = false;
let panStart = { x: 0, y: 0 };
let panOffset = { x: 0, y: 0 };
// Zoom بعجلة الماوس
container.addEventListener('wheel', (event) => {
    event.preventDefault();
    const zoomSpeed = 0.95;
    if (event.deltaY < 0) {
        zoom /= zoomSpeed;
    } else {
        zoom *= zoomSpeed;
    }
    zoom = Math.max(minZoom, Math.min(zoom, 50));
    updateCamera();
});

function updateCamera() {
    const size = frustumSize / zoom;
    camera.left = (-size * aspect / 2) + panOffset.x;
    camera.right = (size * aspect / 2) + panOffset.x;
    camera.top = (size / 2) + panOffset.y;
    camera.bottom = (-size / 2) + panOffset.y;
    camera.updateProjectionMatrix();

    console.log('📷 Camera updated:', { zoom, panOffset, size });
}

// Pan بالزر الأيمن أو الأوسط
container.addEventListener('mousedown', (event) => {
    if (event.button === 2 || event.button === 1) { // زر أيمن أو أوسط
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

// منع القائمة السياقية عند الضغط بالزر الأيمن
container.addEventListener('contextmenu', (event) => {
    event.preventDefault();
});
container.addEventListener('click', onMouseClick);
// تحديث الأبعاد عند تغيير حجم النافذة
window.addEventListener('resize', () => {
    // تحديث الأبعاد
    width = container.clientWidth;
    height = container.clientHeight;
    aspect = width / height;

    // تحديث حجم الريندر
    renderer.setSize(width, height);

    // تحديث الكاميرا (بنستدعي الدالة اللي عملناها عشان تحافظ على الزوم والمكان)
    updateCamera();
});