$(document).ready(function () {
    /* -------------------------
       Section Navigation
    ------------------------- */
    function openSectionFromHash() {
        const hash = window.location.hash.substring(1);
        if (!hash) return;

        $(".content-section").removeClass("active");
        $(`#${hash}`).addClass("active");

        $(".sidebar .nav-link").removeClass("active");
        $(`.sidebar .nav-link[data-section="${hash}"]`).addClass("active");
    }

    openSectionFromHash();
    $(window).on("hashchange", openSectionFromHash);

    $(document).on("click", ".sidebar .nav-link[data-section]", function (e) {
        e.preventDefault();
        const target = $(this).data("section");
        window.location.hash = target;
    });

    /* -------------------------
       Audio Player
    ------------------------- */
    let currentAudio = null;

    $(document).on("click", ".btn[data-music]", function () {
        const btn = $(this);
        const icon = btn.find("i");
        const musicUrl = btn.data("music");

        if (currentAudio && !currentAudio.paused && currentAudio.src.includes(musicUrl)) {
            currentAudio.pause();
            icon.removeClass("bi-pause-fill").addClass("bi-play-fill");
            btn.removeClass("btn-primary").addClass("btn-outline-primary");
            return;
        }

        if (currentAudio) {
            currentAudio.pause();
            const prevBtn = $(".btn.btn-primary");
            prevBtn.removeClass("btn-primary").addClass("btn-outline-primary")
                .find("i").removeClass("bi-pause-fill").addClass("bi-play-fill");
        }

        currentAudio = new Audio(musicUrl);
        currentAudio.play();

        icon.removeClass("bi-play-fill").addClass("bi-pause-fill");
        btn.removeClass("btn-outline-primary").addClass("btn-primary");

        currentAudio.addEventListener("ended", function () {
            icon.removeClass("bi-pause-fill").addClass("bi-play-fill");
            btn.removeClass("btn-primary").addClass("btn-outline-primary");
        });
    });

    /* -------------------------
        Expand Collapse Item
    ------------------------- */
    window.loadAlbums = async function (artistId, button) {
        const container = document.querySelector(`#artist-${artistId} .albums-container`);

        if (container.classList.contains("visible")) {
            //console.log("Collapsing");
            container.classList.remove("visible");
            button.innerText = "Expand";
            // Wait until the transition finishes before hiding
            setTimeout(() => {
                setVisibility(container, false);
                container.innerHTML = "";
            }, 400); // match the CSS transition duration
        } else {
            //console.log("Expanding");
            setVisibility(container, true);
            button.innerText = "Collapse";
            requestAnimationFrame(() => container.classList.add("visible"));
        }


        const response = await fetch(`/Song/GetArtistAlbum?artistid=${artistId}`);
        const albums = await response.json();

        if (albums.length === 0) {
            container.innerHTML = "<p class='text-muted'>No albums found.</p>";
            return;
        }

        container.innerHTML = "";
        for (const album of albums) {

            const albumContainer = document.createElement("div");
            albumContainer.className = "col-md-12 mt-3";
            const albumcard = document.createElement("div");
            albumcard.className = "card p-3";

            const albumDiv = document.createElement("div");
            albumDiv.className = "d-flex align-items-center justify-content-between";
            albumDiv.innerHTML = `
            <div class="d-flex align-items-center">
                <img src="${album.coverImagePath || '/images/default.png'}"
                     class="rounded-3 me-3" style="width: 80px; height: 80px; object-fit: cover;" />
                <div>
                    <h5 class="mb-1">${album.title}</h5>
                    <small class="text-muted">${album.releaseDate ?? ''}</small>
                </div>
            </div>
            <div class="btn-group">
                <button class="btn btn-sm btn-outline-info" onclick="loadSongs(${album.id}, this)">Expand</button>
                <a class="btn btn-sm btn-outline-primary">Edit</a>
                <a class="btn btn-sm btn-outline-danger">Delete</a>
            </div>
        `;

            const songsContainer = document.createElement("div");
            songsContainer.className = "p-3 mt-2 songs-container";
            songsContainer.style = "display: none";

            albumcard.appendChild(albumDiv);
            albumcard.appendChild(songsContainer);
            albumContainer.appendChild(albumcard);
            container.appendChild(albumContainer);
        }
    };
    window.setVisibility = function (element, visible) {
        element.style.display = visible ? "block" : "none";
    }

    window.loadSongs = async function (albumId, button) {
        const card = button.closest(".card");
        const container = card.querySelector(".songs-container");
        if (!container) {
            console.warn("No songs-container found for this album card.");
            return;
        }
        if (container.classList.contains("visible")) {
            //console.log("Collapsing");
            container.classList.remove("visible");
            button.innerText = "Expand";
            // Wait until the transition finishes before hiding
            setTimeout(() => {
                setVisibility(container, false);
                container.innerHTML = "";
            }, 400); // match the CSS transition duration
        } else {
            //console.log("Expanding");
            setVisibility(container, true);
            button.innerText = "Collapse";
            requestAnimationFrame(() => container.classList.add("visible"));
        }

        const response = await fetch(`/Song/GetAlbumSong?albumid=${albumId}`);
        const songs = await response.json();

        if (!songs || songs.length === 0) {
            container.innerHTML = "<p class='text-muted'>No songs found.</p>";
            return;
        }
        
        const tableWrapper = document.createElement("div");
        tableWrapper.className = "table-responsive";

        const table = document.createElement("table");
        table.className = "table table-dark table-hover";
        table.innerHTML = `
        <thead>
            <tr>
                <th>Song</th>
                <th class="text-end">Duration</th>
                <th class="text-end">Plays</th>
                <th class="text-end">Likes</th>
                <th class="text-end">Actions</th>
            </tr>
        </thead>
        <tbody>
            ${songs.map(m => `
                <tr>
                    <td>
                        <div class="d-flex align-items-center">
                            <button class="btn btn-sm btn-outline-primary me-3" data-music="${m.musicFilePath}">
                                <i class="bi bi-play-fill"></i>
                            </button>
                            <div><strong>${m.title}</strong></div>
                        </div>
                    </td>
                    <td class="text-end">${Math.floor(m.duration / 60)}:${(m.duration % 60).toString().padStart(2, '0')}</td>
                    <td class="text-end">${m.plays}</td>
                    <td class="text-end">${m.likes}</td>
                    <td class="text-end">
                        <button class="btn btn-sm btn-outline-primary me-1"><i class="bi bi-pencil"></i></button>
                        <button class="btn btn-sm btn-outline-danger"><i class="bi bi-trash"></i></button>
                    </td>
                </tr>
            `).join('')}
        </tbody>
    `;

        tableWrapper.appendChild(table);

        container.innerHTML = "";
        container.appendChild(tableWrapper);
    };
    /* -------------------------
        Artist Modal
    ------------------------- */
    window.openArtistModal = function (id = 0, artistName = '', artistBio = '', artistProfileUrl = '') {
        const modal = new bootstrap.Modal(document.getElementById('artistModal'));
        const form = document.getElementById('artistForm');
        const fileInput = form.querySelector('#artistProfileUrl');
        const preview = form.querySelector('#artistProfilePreview');

        // Reset form and preview
        form.reset();
        preview.src = '';
        preview.classList.add('d-none');

        // Fill form values
        form.querySelector('#artistId').value = id || '';
        form.querySelector('#artistName').value = artistName || '';
        form.querySelector('#artistBio').value = artistBio || '';

        // Show existing profile picture if provided
        if (artistProfileUrl) {
            preview.src = artistProfileUrl;
            preview.classList.remove('d-none');
        }

        // Live preview when selecting new file
        fileInput.onchange = (e) => {
            const file = e.target.files[0];
            if (file) {
                preview.src = URL.createObjectURL(file);
                preview.classList.remove('d-none');
            } else {
                preview.src = '';
                preview.classList.add('d-none');
            }
        };

        modal.show();
    };
});

document.getElementById("artistForm").addEventListener("submit", async function (e) {
    e.preventDefault();

    const formData = new FormData(this);
    const response = await fetch("/Artist/Edit", {
        method: "POST",
        body: formData
    });

    if (response.ok) {
        bootstrap.Modal.getInstance(document.getElementById("artistModal")).hide();
        alert("Artist saved successfully!");
        // reload or update table if needed
    } else {
        const error = await response.text();
        alert("Error: " + error);
    }
});