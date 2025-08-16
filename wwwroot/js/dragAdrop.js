function initializeDragAndDrop() {
    const container = document.getElementById('previewContainer');


    const attachCancelListener = (imgContainer) => {
        const cancelButton = imgContainer.querySelector(".cancel-button");
        if (cancelButton) {
            cancelButton.addEventListener("click", () => {
                imgContainer.classList.add("fade-out");
                setTimeout(() => {
                    imgContainer.remove();

                    // Recalculate sequence numbers after removal
                    initializeSequence();
                    updateImageOrder();
                }, 300);
            });
        }
    };

    // Attach listeners for preloaded images
    document.querySelectorAll('.image-container').forEach(container => {
        attachCancelListener(container);
    });
    const imageContainers = container.querySelectorAll('.image-container');
    const preloadedImages = document.querySelectorAll('.image-container');
    preloadedImages.forEach(container => attachCancelListener(container));
    // Initialize sequence numbers
    function initializeSequence() {
        const images = container.querySelectorAll('.image-container');
        images.forEach((container, index) => {
            // Add sequence attribute
            container.setAttribute('data-sequence', index + 1);

            // Add visible sequence number
            let sequenceDisplay = container.querySelector('.sequence-number');
            if (!sequenceDisplay) {
                sequenceDisplay = document.createElement('div');
                sequenceDisplay.className = 'sequence-number';
                container.appendChild(sequenceDisplay);
            }
            sequenceDisplay.textContent = index + 1;
        });
    }

    imageContainers.forEach(container => {
        container.addEventListener('dragstart', handleDragStart);
        container.addEventListener('dragover', handleDragOver);
        container.addEventListener('drop', handleDrop);
        container.addEventListener('dragenter', handleDragEnter);
        container.addEventListener('dragleave', handleDragLeave);
    });
    const dropbox = document.getElementById("dropbox");
    const fileInput = document.getElementById("Images");
    // const previewContainer = document.getElementById("previewContainer");
    const selectImagesButton = document.getElementById("selectImages");
    // document.getElementById("RoomQuantity").disabled = true;
    // Handle drag-and-drop functionality
    dropbox.addEventListener("dragover", (e) => {
        e.preventDefault();
        dropbox.style.backgroundColor = "#e6e6e6";
    });

    dropbox.addEventListener("dragleave", () => {
        dropbox.style.backgroundColor = "#f9f9f9";
    });

    dropbox.addEventListener("drop", (e) => {
        e.preventDefault();
        dropbox.style.backgroundColor = "#f9f9f9";
        const files = Array.from(e.dataTransfer.files);
        handleFiles(files);
    });

    // Open file dialog when clicking the "Select Images" button
    selectImagesButton.addEventListener("click", () => {
        fileInput.click();
    });

    // Handle file selection through the input
    fileInput.addEventListener("change", (e) => {
        const files = Array.from(e.target.files);
        handleFiles(files);
        validateImages();
    });

    const handleFiles = (files) => {
        const maxFiles = 5;
        const existingImages = previewContainer.querySelectorAll('.image-container');
        if (existingImages.length + files.length > maxFiles) {
            Swal.fire({
                icon: "error",
                title: "Limit Exceeded",
                text: `You can upload a maximum of ${maxFiles} images.`,
            });
            return;
        }

        files.forEach((file) => {
            if (!file.type.match("image.*")) {
                Swal.fire({
                    icon: "error",
                    title: "Invalid File",
                    text: "Only image files are allowed.",
                });
                return;
            }

            if (!window.FileReader) {
                console.error("FileReader is not supported in this browser.");
                return;
            }

            const maxFileSize = 2 * 1024 * 1024;
            if (file.size > maxFileSize) {
                Swal.fire({
                    icon: "error",
                    title: "File Too Large",
                    text: `${file.name} must be less than 2 MB.`,
                });
                return;
            }

            const reader = new FileReader();
            reader.onload = (e) => {
                const imgContainer = document.createElement("div");
                imgContainer.classList.add("image-container");
                imgContainer.setAttribute("draggable", "true");

                const img = document.createElement("img");
                img.src = e.target.result;
                img.alt = "Room Image";
                img.classList.add("preview-image");
                img.style.width = "100px";
                img.style.height = "100px";
                img.style.cursor = "pointer";
                imgContainer.appendChild(img);

                const cancelButton = document.createElement("button");
                cancelButton.textContent = "✖";
                cancelButton.classList.add("cancel-button");
                cancelButton.addEventListener("click", () => {
                    imgContainer.classList.add("fade-out");
                    setTimeout(() => {
                        imgContainer.remove();
                        // updateSequence();
                        updateImageOrder();
                    }, 300);
                });

                imgContainer.appendChild(cancelButton);
                previewContainer.appendChild(imgContainer);

                // Add drag-drop listeners to new container
                imgContainer.addEventListener('dragstart', handleDragStart);
                imgContainer.addEventListener('dragover', handleDragOver);
                imgContainer.addEventListener('drop', handleDrop);
                imgContainer.addEventListener('dragenter', handleDragEnter);
                imgContainer.addEventListener('dragleave', handleDragLeave);

                // Update sequence after adding new image
                initializeSequence();
                updateImageOrder();
            };

            reader.readAsDataURL(file);
        });
    };


    let draggedElement = null;

    function handleDragStart(e) {
        draggedElement = this;
        this.style.opacity = '0.4';
        this.classList.add('dragging');

        // Store the current sequence
        e.dataTransfer.setData('text/plain', this.getAttribute('data-sequence'));
    }

    function handleDragOver(e) {
        e.preventDefault();
        return false;
    }

    function handleDragEnter(e) {
        e.preventDefault();
        this.classList.add('drag-over');
    }

    function handleDragLeave(e) {
        this.classList.remove('drag-over');
    }

    function handleDrop(e) {
        e.preventDefault();

        if (draggedElement !== this) {
            let items = [...container.querySelectorAll('.image-container')];
            const fromIndex = items.indexOf(draggedElement);
            const toIndex = items.indexOf(this);

            if (fromIndex < toIndex) {
                this.parentNode.insertBefore(draggedElement, this.nextSibling);
            } else {
                this.parentNode.insertBefore(draggedElement, this);
            }

            updateSequence();
            updateImageOrder();
        }

        this.classList.remove('drag-over');
        draggedElement.style.opacity = '1';
        draggedElement.classList.remove('dragging');
        draggedElement = null;

        return false;
    }

    function updateSequence() {
        const images = container.querySelectorAll('.image-container');
        images.forEach((container, index) => {
            container.setAttribute('data-sequence', index + 1);
            const sequenceDisplay = container.querySelector('.sequence-number');
            if (sequenceDisplay) {
                sequenceDisplay.textContent = index + 1;
            }
        });
    }

    function getImageFilename(imgElement) {
        const src = imgElement.getAttribute('src');
        return src.split('/').pop();
    }

    function updateImageOrder() {
        // Remove existing hidden inputs
        const existingInputs = container.querySelectorAll('input[name="ExistingPreviews"]');
        existingInputs.forEach(input => input.remove());

        // Create new hidden inputs in the current order
        const images = container.querySelectorAll('.image-container');
        const imageOrder = [];

        images.forEach((container, index) => {
            const img = container.querySelector('img');
            const filename = getImageFilename(img);
            const sequence = index + 1;

            // Create hidden input for filename
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'ExistingPreviews';
            input.value = filename;
            container.appendChild(input);

            // Store the order information
            imageOrder.push({
                filename: filename,
                sequence: sequence
            });
        });

        // Log the current order for verification
        console.log('Current Image Order:', imageOrder);

        // Optionally send to server
        // sendOrderToServer(imageOrder);
    }

    // Initialize sequence numbers on load
    initializeSequence();


}

// Add the required CSS
const style = document.createElement('style');
style.textContent = `
            .image-container {
                position: relative;
            }

            .sequence-number {
                position: absolute;
                top: 5px;
                left: 5px;
                background-color: rgba(0, 0, 0, 0.7);
                color: white;
                border-radius: 50%;
                width: 20px;
                height: 20px;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 12px;
                font-weight: bold;
            }

            .dragging .sequence-number {
                background-color: #4a90e2;
            }
        `;
document.head.appendChild(style);

function validateImages() {
    const inputElement = document.getElementById("Images");
    const errMsg = document.getElementById("ImgErr");
    const maxFiles = 5; // Maximum allowed files
    const maxSize = 2 * 1024 * 1024; // Maximum file size (2 MB)
    const allowedTypes = ["image/jpeg", "image/png", "image/gif"]; // Allowed file types
    const files = inputElement.files;
    let previewContainer = document.getElementById("previewContainer");
    const imageContainers = previewContainer.getElementsByClassName("image-container");
    // Clear any previous error messages
    errMsg.innerHTML = "";

    // Check if no files are selected
    if (files.length === 0 && imageContainers.length === 0) {
        errMsg.innerHTML = "Please select at least one image.";
        return false;
    } else

        // Check file count
        if ((files.length > maxFiles) && imageContainers.length > maxFiles) {
            errMsg.innerHTML = `You can upload a maximum of ${maxFiles} images.`;
            return false;
        } else {

            // Validate each file
            for (let i = 0; i < files.length; i++) {
                const file = files[i];

                // Check file type
                if (!allowedTypes.includes(file.type)) {
                    errMsg.innerHTML = `File type not allowed: ${file.name}`;
                    return false;
                }

                // Check file size
                if (file.size > maxSize) {
                    errMsg.innerHTML = `File size exceeds 2MB: ${file.name}`;
                    return false;
                }
            }
        }
    return true;
}


// Initialize when the document is ready
