// 1. image lazy loading

window.initLazyLoading = function () {
    const observer = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                img.src = img.dataset.src;
                img.classList.remove('lazy-image');
                observer.unobserve(img);
            }
        });
    });

    document.querySelectorAll('img.lazy-image').forEach(img => {
        observer.observe(img);
    });
}


// 2. Editor

// 2.1 get editor with image for blob 
window.getTinyMCEContent = async (textareaId) => {
    try {
        var editor = tinymce.get(textareaId);
        if (editor) {

            var content = editor.getContent();

            // base64 to blob
            content = content.replace(/<img[^>]+src="data:image\/([^;]+);base64,([^"]+)"/g, (match, mimeType, base64Data) => {

                var blobUrl = window.createBlobUrlFromBase64(base64Data, 'image/' + mimeType);

                return match.replace(`src="data:image/${mimeType};base64,${base64Data}"`, `src="${blobUrl}"`);
            });

            return content;
        } else {
            return "";
        }
    } catch (error) {
        console.error("Error in getTinyMCEContent:", error);
        return "";
    }
};


// 2.2 convert base64 to blob
window.createBlobUrlFromBase64 = function (base64, contentType) {

    base64 = base64.replace(/[^A-Za-z0-9+/=]/g, '');

    try {
        const byteCharacters = atob(base64);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);

        const blob = new Blob([byteArray], { type: contentType });
        return URL.createObjectURL(blob);
    } catch (error) {
        console.error("Error while creating Blob URL:", error);
        return "";
    }
};

// 2.2 convert file buffer to blob
window.createBlobUrlFromBuffer = (data, contentType) => {
    const blob = new Blob([new Uint8Array(data)], { type: contentType });
    return URL.createObjectURL(blob);
};

// 2.3 init TinyMCE for Editor
window.initTinyMCE = (selector, content = '') => {
    const existingEditor = tinymce.get(selector.replace('#', ''));

    if (existingEditor) {
        existingEditor.remove();
    }

    tinymce.init({
        selector: selector || 'textarea',
        plugins: [
            'image', 'link', 'code', 'table'    
        ],
        menubar: false,
        toolbar: 'fontfamily fontsize forecolor backcolor uploadImageButton bold italic underline | alignleft aligncenter alignright alignjustify | numlist bullist outdent indent',
        toolbar_mode: 'wrap',
        mobile: {
            toolbar_mode: 'wrap',
            toolbar: 'fontfamily fontsize forecolor backcolor | bold italic underline | alignleft aligncenter alignright alignjustify | numlist bullist | removeformat'
        },
        content_style: `

        `,
        setup: function (editor) {
            editor.on('init', function () {

                const editorContainer = editor.getContainer();
                editorContainer.style.minHeight = '500px';

                // set the height based on total screen
                //const setEditorHeight = () => {
                //    const windowHeight = window.innerHeight;
                //    const offsetTop = editorContainer.getBoundingClientRect().top;
                //    const newHeight = windowHeight - offsetTop - 20; // Add padding/margin as needed
                //    editorContainer.style.height = `${newHeight}px`;
                //};

                //// Initialize and adjust height on window resize
                //setEditorHeight();
                //window.addEventListener('resize', setEditorHeight);

                // Upload Image Custom Button
                editor.ui.registry.addButton('uploadImageButton', {
                    tooltip: 'Click to upload an image',
                    icon: 'image',
                    onAction: function () {
                        const fileInput = document.createElement('input');
                        fileInput.type = 'file';
                        fileInput.accept = 'image/*';
                        fileInput.onchange = function (e) {
                            const file = e.target.files[0];
                            if (file) {
                                const reader = new FileReader();
                                reader.onload = (e) => {
                                    const base64String = e.target.result;
                                    editor.insertContent(`<img src="${base64String}" alt="${file.name}" style="max-width:100%; height:auto; object-fit:cover; border-radius:8px; margin:1rem 0;" />`);
                                };
                                reader.readAsDataURL(file); // 파일을 Base64로 변환
                            }
                            //if (file) {
                            //    const contentType = file.type;

                            //    const blobUrl = URL.createObjectURL(file);

                            //    // Blob URL을 에디터에 삽입
                            //    editor.insertContent(`<img src="${blobUrl}" alt="${file.name}" />`);

                            //    console.log(blobUrl); // Blob URL 확인
                            //}
                        };
                        fileInput.click();
                    }
                });

                if (content) {
                    editor.setContent(content); // Set content if provided
                }

            });

            //editor.on('change', function () {
            //    const content = editor.getContent();
            //    saveContent(content);
            //});
        }
    });
};


//function saveContent(content) {
//    console.log('Saved Content:', content);
//}


// 2.4 convert image for byte[]?
function fetchBlobData(blobUrl) {
    return fetch(blobUrl)
        .then(response => response.arrayBuffer())
        .then(buffer => {
            const base64String = btoa(String.fromCharCode(...new Uint8Array(buffer)));
            return base64String;
        });
}