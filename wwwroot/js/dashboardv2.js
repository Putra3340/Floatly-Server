$(document).ready(function () {
    const encode = (val) =>
        encodeURIComponent(val ?? "").replace(/'/g, "\\'");
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

    window.loadArtists = async function (start = 0, end = pageSize) {
        console.log("Loading Artist...");

        const container = document.querySelector("#artists-container");
        container.innerHTML = "<p class='text-muted'>Loading artists...</p>";

        const response = await fetch(`/Song/GetArtist?start=${start}&end=${end}`);
        const artists = await response.json();

        if (!artists.length) {
            container.innerHTML = "<p class='text-muted'>No artists found.</p>";
            return;
        }

        container.innerHTML = "";

        

        for (const artist of artists) {
            const safeName = encode(artist.name);
            const safeBio = encode(artist.bio);
            const safeCover = encode(artist.coverImagePath);

            const artistCard = document.createElement("div");
            artistCard.className = "col-md-12 mb-3 artist-card";
            artistCard.id = `artist-${artist.id}`;

            artistCard.innerHTML = `
            <div class="card p-3">
                <div class="d-flex align-items-center justify-content-between">
                    <div class="d-flex align-items-center">
                        ${artist.coverImagePath
                    ? `<img src="${artist.coverImagePath}" class="rounded-circle me-3" style="width: 80px; height: 80px; object-fit: cover;" />`
                    : `<div class="bg-primary rounded-circle d-flex align-items-center justify-content-center me-3"
                                       style="width: 80px; height: 80px;">
                                       <i class="bi bi-person fs-1 text-white"></i>
                                   </div>`
                }
                        <div>
                            <h5 class="mb-1">${artist.name}</h5>
                            <small class="text-muted">
                                ${artist.bio?.length > 100
                    ? artist.bio.substring(0, 180) + "..."
                    : artist.bio || ""}
                            </small>
                            <div class="text-muted small mt-1">
                                <strong>${artist.albumCount}</strong> Albums - 
                                <strong>${artist.songCount}</strong> Songs
                            </div>
                        </div>
                    </div>

                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-info" onclick="loadAlbums(${artist.id}, this)">Expand</button>
                        <a class="btn btn-sm btn-outline-primary"
                           onclick="openArtistModal(${artist.id},
                               decodeURIComponent('${safeName}'),
                               decodeURIComponent('${safeBio}'),
                               decodeURIComponent('${safeCover}')
                           )">Edit</a>
                        <button class="btn btn-sm btn-outline-warning" 
                            onclick='openAlbumModal(
                                0,
                                ${artist.id},
                            )'>Add</button>
                        <button type="button" class="btn btn-outline-danger btn-sm delete-btn" onclick="openDeleteModal(${artist.id},1,decodeURIComponent('${safeName}'))">Delete</button>
                    </div>
                </div>

                <div class="row albums-container" style="display: none"></div>
            </div>
        `;

            container.appendChild(artistCard);
        }
    };
    window.loadSearch = function (query = "") {
        console.log("Loading Artist...");
        if (query === "" || query.length <3) {
            loadArtists();
            return;
        }

        const container = document.querySelector("#artists-container");
        container.innerHTML = "<p class='text-muted'>Loading artists...</p>";

        fetch(`/Song/GetLibrarySearch?query=${encodeURIComponent(query)}`)
            .then(response => {
                if (!response.ok) throw new Error("Failed to fetch artists");
                return response.json();
            })
            .then(artists => {
                if (!artists.length) {
                    container.innerHTML = "<p class='text-muted'>No artists found.</p>";
                    return;
                }

                container.innerHTML = "";

                const artistPromises = artists.map(artist => {
                    const safeName = encode(artist.name);
                    const safeBio = encode(artist.bio);
                    const safeCover = encode(artist.coverImagePath);

                    const artistCard = document.createElement("div");
                    artistCard.className = "col-md-12 mb-3 artist-card";
                    artistCard.id = `artist-${artist.id}`;
                    artistCard.innerHTML = `
                    <div class="card p-3">
                        <div class="d-flex align-items-center justify-content-between">
                            <div class="d-flex align-items-center">
                                ${artist.coverImagePath
                            ? `<img src="${artist.coverImagePath}" class="rounded-circle me-3" style="width:80px;height:80px;object-fit:cover;" />`
                            : `<div class="bg-primary rounded-circle d-flex align-items-center justify-content-center me-3"
                                        style="width:80px;height:80px;">
                                        <i class="bi bi-person fs-1 text-white"></i>
                                    </div>`}
                                <div>
                                    <h5 class="mb-1">${artist.name}</h5>
                                    <small class="text-muted">
                                        ${artist.bio?.length > 100 ? artist.bio.substring(0, 180) + "..." : artist.bio || ""}
                                    </small>
                                    <div class="text-muted small mt-1">
                                        <strong>${artist.albumCount}</strong> Albums - 
                                        <strong>${artist.songCount}</strong> Songs
                                    </div>
                                </div>
                            </div>
                            <div class="btn-group">
                                <button class="btn btn-sm btn-outline-info" onclick="loadAlbums(${artist.id}, this)">Collapse</button>
                                <a class="btn btn-sm btn-outline-primary"
                                   onclick="openArtistModal(${artist.id},
                                       decodeURIComponent('${safeName}'),
                                       decodeURIComponent('${safeBio}'),
                                       decodeURIComponent('${safeCover}')
                                   )">Edit</a>
                                <button class="btn btn-sm btn-outline-warning" onclick="openAlbumModal(0, ${artist.id})">Add</button>
                                <button type="button" class="btn btn-outline-danger btn-sm delete-btn" onclick="openDeleteModal(${artist.id},1,decodeURIComponent('${safeName}'))">Delete</button>
                            </div>
                        </div>
                        <div class="row albums-container visible" style="display:block"></div>
                    </div>
                `;
                    container.appendChild(artistCard);

                    // Fetch albums in parallel
                    return fetch(`/Song/GetArtistAlbum?artistid=${artist.id}`)
                        .then(res => res.json())
                        .then(albums => {
                            const albumsContainer = artistCard.querySelector(".albums-container");
                            if (!albums.length) {
                                albumsContainer.innerHTML = "<p class='text-muted'>No albums found.</p>";
                                return;
                            }

                            const albumPromises = albums.map(album => {
                                const albumContainer = document.createElement("div");
                                albumContainer.className = "col-md-12 mt-3";
                                albumContainer.innerHTML = `
                                <div class="card p-3" id="album-${album.id}">
                                    <div class="d-flex align-items-center justify-content-between">
                                        <div class="d-flex align-items-center">
                                            <img src="${album.coverImagePath || '/images/default.png'}"
                                                class="rounded-3 me-3" style="width:80px;height:80px;object-fit:cover;" />
                                            <div>
                                                <h5 class="mb-1">${album.title}</h5>
                                                <small class="text-muted">${album.releaseDate ?? ''}</small>
                                            </div>
                                        </div>
                                        <div class="btn-group">
                                            <button class="btn btn-sm btn-outline-info" onclick="loadSongs(${album.id}, this)">Collapse</button>
                                            <a class="btn btn-sm btn-outline-primary"
                                                onclick="openAlbumModal(${album.id}, ${artist.id},
                                                    decodeURIComponent('${encode(album.title)}'),
                                                    decodeURIComponent('${encode(album.releaseDate)}'),
                                                    decodeURIComponent('${encode(album.coverImagePath)}'))">Edit</a>
                                            <a class="btn btn-sm btn-outline-warning" onclick="openSongModal(0,${album.id})">Add</a>
                                            <button type="button" class="btn btn-outline-danger btn-sm delete-btn" onclick="openDeleteModal(${album.id},2,decodeURIComponent('${encode(album.title)}'),${artist.id})">Delete</button>
                                        </div>
                                    </div>
                                    <div class="p-3 mt-2 songs-container visible" style="display:block"></div>
                                </div>
                            `;
                                albumsContainer.appendChild(albumContainer);

                                // Fetch songs in parallel
                                return fetch(`/Song/GetAlbumSong?albumid=${album.id}`)
                                    .then(songRes => songRes.json())
                                    .then(songs => {
                                        const songsContainer = albumContainer.querySelector(".songs-container");
                                        if (!songs.length) {
                                            songsContainer.innerHTML = "<p class='text-muted'>No songs found.</p>";
                                            return;
                                        }

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
                                                    <td>${m.title}</td>
                                                    <td class="text-end">${Math.floor(m.duration / 60)}:${(m.duration % 60).toString().padStart(2, '0')}</td>
                                                    <td class="text-end">${m.plays}</td>
                                                    <td class="text-end">${m.likes}</td>
                                                    <td class="text-end">
                                                        <button class="btn btn-sm btn-outline-primary me-1" onclick="openSongModal(${m.id},${m.albumId})"><i class="bi bi-pencil"></i></button>
                                                        <button class="btn btn-sm btn-outline-danger" onclick="openDeleteModal(${m.id},3,decodeURIComponent('${encode(m.title)}'),0,${m.albumId})"><i class="bi bi-trash"></i></button>
                                                    </td>
                                                </tr>`).join("")}
                                        </tbody>
                                    `;
                                        songsContainer.appendChild(table);
                                    });
                            });

                            return Promise.all(albumPromises);
                        });
                });

                return Promise.all(artistPromises);
            })
            .catch(error => {
                console.error(error);
                container.innerHTML = "<p class='text-danger'>Failed to load artists.</p>";
            });
    };

    window.loadAlbums = async function (artistId, button = null) {
        const container = document.querySelector(`#artist-${artistId} .albums-container`);

        if (button != null) {
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
        }


        const response = await fetch(`/Song/GetArtistAlbum?artistid=${artistId}`);
        const albums = await response.json();

        if (albums.length === 0) {
            container.innerHTML = "<p class='text-muted'>No albums found.</p>";
            return;
        }

        container.innerHTML = "";
        for (const album of albums) {
            const safeTitle = encode(album.title);
            const safeDate = encode(album.releaseDate);
            const safeCover = encode(album.coverImagePath);


            const albumContainer = document.createElement("div");
            albumContainer.className = "col-md-12 mt-3";
            const albumcard = document.createElement("div");
            albumcard.className = "card p-3";
            albumcard.id = `album-${album.id}`;

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
                <a class="btn btn-sm btn-outline-primary"
                   onclick="openAlbumModal(${album.id},
                       ${artistId},
                       decodeURIComponent('${safeTitle}'),
                       decodeURIComponent('${safeDate}'),
                       decodeURIComponent('${safeCover}')
                   )">Edit</a>
                   <a class="btn btn-sm btn-outline-warning"
                   onclick="openSongModal(0,${album.id})">Add</a>

                <button type="button" class="btn btn-outline-danger btn-sm delete-btn" onclick="openDeleteModal(${album.id},2,decodeURIComponent('${safeTitle}'),${artistId})">Delete</button>
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
    window.loadSongs = async function (albumId, button = null) {
        container = document.querySelector(`#album-${albumId} .songs-container`);
        if (button != null) {
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
            ${songs.map(m => {
                const safeTitle = encode(m.title);
                const safeCover = encode(m.coverUrl);
                const safeBanner = encode(m.bannerUrl);
                const safeVideo = encode(m.videoUrl);

                return `
                <tr>
                    <td>
                        <div class="d-flex align-items-center">
                            <button class="btn btn-sm btn-outline-primary me-3" data-music="${m.musicUrl}">
                                <i class="bi bi-play-fill"></i>
                            </button>
                            <div><strong>${m.title}</strong></div>
                        </div>
                    </td>
                    <td class="text-end">${Math.floor(m.duration / 60)}:${(m.duration % 60).toString().padStart(2, '0')}</td>
                    <td class="text-end">${m.plays}</td>
                    <td class="text-end">${m.likes}</td>
                    <td class="text-end">
                        <button class="btn btn-sm btn-outline-primary me-1" onclick="openSongModal(${m.id},${m.albumId},decodeURIComponent('${safeTitle}'),decodeURIComponent('${safeCover}'),decodeURIComponent('${safeBanner}'),decodeURIComponent('${safeVideo}'))"><i class="bi bi-pencil"></i></button>
                        <button class="btn btn-sm btn-outline-danger" onclick="openDeleteModal(${m.id},3,decodeURIComponent('${safeTitle}'),0,${m.albumId})"><i class="bi bi-trash"></i></button>
                    </td>
                </tr>
            `;
            }).join('')}
        </tbody>
    `;

        tableWrapper.appendChild(table);

        container.innerHTML = "";
        container.appendChild(tableWrapper);
    };



    /* -------------------------
        Toast Notification
    ------------------------- */
    window.showToast = function (message, type = 'success') {
        const toastEl = document.getElementById('toastSuccess');
        const toastBody = toastEl.querySelector('.toast-body');

        // update message & color
        toastBody.textContent = message;
        toastEl.className = `toast align-items-center text-bg-${type} border-0`;
        console.log(message);
        // show it~
        const toast = new bootstrap.Toast(toastEl);
        toast.show();
    };

    /* -------------------------
        Artist Modal
    ------------------------- */
    window.openArtistModal = function (id = 0, artistName = '', artistBio = '', artistProfileUrl = '') {
        const modalElement = document.getElementById('artistModal');
        const modal = new bootstrap.Modal(modalElement);
        const form = document.getElementById('artistForm');
        const fileInput = form.querySelector('#artistProfileUrl');
        const preview = form.querySelector('#artistProfilePreview');

        const titleElement = modalElement.querySelector('.modal-title');
        titleElement.textContent = id === 0 ? 'Add Artist' : 'Edit Artist';

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

        setTimeout(() => modal.show(), 100);

    };

    /* -------------------------
        Album Modal
    ------------------------- */
    window.openAlbumModal = function (id = 0, artistId = 0, albumTitle = '',albumReleaseDate = '', albumCoverUrl = '') {
        const modalElement = document.getElementById('albumModal');
        const modal = new bootstrap.Modal(modalElement);
        const form = modalElement.querySelector('#albumForm');
        const fileInput = form.querySelector('#albumCoverUrl');
        const preview = modalElement.querySelector('#albumCoverPreview'); // optional preview if added later

        const titleElement = modalElement.querySelector('.modal-title');
        titleElement.textContent = id === 0 ? 'Add Album' : 'Edit Album';

        // Reset form
        form.reset();

        // Fill in the values
        form.querySelector('#albumId').value = id || '';
        form.querySelector('input[name="artistId"]').value = artistId || '';
        form.querySelector('#albumTitle').value = albumTitle || '';
        form.querySelector('#albumReleaseDate').value = albumReleaseDate || '';

        // Show existing cover preview if provided (optional)
        if (preview && albumCoverUrl) {
            preview.src = albumCoverUrl;
            preview.classList.remove('d-none');
        } else if (preview) {
            preview.classList.add('d-none');
        }

        // File preview (if preview image exists in DOM)
        if (preview) {
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
        }

        setTimeout(() => modal.show(), 100);
    };

    /* -------------------------
        Universal Delete Modal
    ------------------------- */
    window.openDeleteModal = function (id = 0, type = 0, title = '', artistId = 0, albumId = 0) {
        const modalElement = document.getElementById('confirmDeleteModal');
        const modal = new bootstrap.Modal(modalElement);
        const form = modalElement.querySelector('#deleteForm');

        const titleElement = modalElement.querySelector('.modal-title');

        // 1 means artist delete, 2 means album delete, 3 means song delete
        if (type === 0) {
            alert("Aint no way bruh");
        } else if (type === 1) {
            titleElement.textContent = "Delete Artist ?";
        } else if (type === 2) {
            titleElement.textContent = "Delete Album ?";
        } else if (type === 3) {
            titleElement.textContent = "Delete Song ?";
        }

        // Reset form
        form.reset();

        // Fill in the values
        form.querySelector('#id').value = id || '0';
        form.querySelector('#type').value = type || '0';
        form.querySelector('#artistId').value = artistId || '0';
        form.querySelector('#albumId').value = albumId || '0';
        form.querySelector('#deleteName').textContent = title || '0';

        setTimeout(() => modal.show(), 100);
    };

    /* -------------------------
        Song Modal
    ------------------------- */
    window.openSongModal = function (id = 0, albumId = 0, title = '', coverUrl = '', bannerUrl = '', movieUrl = '') {
        const modalElement = document.getElementById('songModal');
        const modal = new bootstrap.Modal(modalElement);
        const form = modalElement.querySelector('#songForm');
        const fileInput = form.querySelector('#coverImage');
        const fileInput2 = form.querySelector('#bannerImage');
        const videoInput = form.querySelector('#specialMovie');
        const preview = form.querySelector('#coverPreview');
        const preview2 = form.querySelector('#bannerPreview');
        const videopreview = form.querySelector('#moviePreview');
        const videosrc = form.querySelector('#moviesrc');
        console.log(movieUrl);

        const titleElement = modalElement.querySelector('.modal-title');
        titleElement.textContent = id === 0 ? 'Add Song' : 'Edit Song';

        // Reset form
        form.reset();

        // Fill in the values
        form.querySelector('#songId').value = id || '';
        form.querySelector('#albumId').value = albumId || '';
        form.querySelector('#songTitle').value = title || '';

        // Preview 1
        if (preview && coverUrl) {
            preview.src = coverUrl;
            preview.classList.remove('d-none');
        } else if (preview) {
            preview.classList.add('d-none');
        }
        // File preview
        if (preview) {
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
        }

        // Preview 2
        if (preview2 && bannerUrl) {
            preview2.src = bannerUrl;
            preview2.classList.remove('d-none');
        } else if (preview2) {
            preview2.classList.add('d-none');
        }
        // File preview2
        if (preview2) {
            fileInput2.onchange = (e) => {
                const file = e.target.files[0];
                if (file) {
                    preview2.src = URL.createObjectURL(file);
                    preview2.classList.remove('d-none');
                } else {
                    preview2.src = '';
                    preview2.classList.add('d-none');
                }
            };
        }

        // Movie preview
        if (videopreview && videosrc && movieUrl) {
            videosrc.src = movieUrl || '';
            videopreview.load();            // refresh the video source
            videopreview.classList.remove('d-none');
        } else if (videopreview) {
            videopreview.classList.add('d-none');
        }
        // File videopreview (if videopreview image exists in DOM)
        if (videopreview) {
            videoInput.onchange = (e) => {
                const file = e.target.files[0];
                if (file) {
                    videosrc.src = URL.createObjectURL(file);
                    videopreview.load(); // refresh to show the new video
                    videopreview.classList.remove('d-none');
                } else {
                    videosrc.src = '';
                    videopreview.load();
                    videopreview.classList.add('d-none');
                }
            };
        }


        setTimeout(() => modal.show(), 100);
    };


    /* -------------------------
        Startup
    ------------------------- */

    let start = 0;
    let end = 9;
    const pageSize = 10; // i think 10 is enough
    let total = 0;

    const artistModal = document.getElementById('artistModal');
    const albumModal = document.getElementById('albumModal');
    const deleteModal = document.getElementById('confirmDeleteModal');
    const songModal = document.getElementById('songModal');

    const el = document.querySelector("a[artisttotal]");
    total = el?.getAttribute("artisttotal");

    updateInputs();
    updateArtists();


    /* -------------------------
            Pagination Thing
        ------------------------- */
    document.getElementById("totalCount").textContent = total;

    function updateArtists() {
        loadArtists(start, end);
    }
    document.getElementById("prevPage").addEventListener("click", () => {
        if (start > 0) {
            start = Math.max(0, start - pageSize);
            end = Math.min(total - 1, start + pageSize - 1);
            updateInputs();
            updateArtists();
        }
    });
    document.getElementById("nextPage").addEventListener("click", () => {
        if (end < total - 1) {
            start = Math.min(total - pageSize, start + pageSize);
            end = Math.min(total - 1, start + pageSize - 1);
            updateInputs();
            updateArtists();
        }
    });
    function updateInputs() {
        document.getElementById("startInput").value = start;
        document.getElementById("endInput").value = end;

        // disable buttons if at edges
        document.getElementById("prevPage").disabled = start <= 0;
        document.getElementById("nextPage").disabled = end >= total - 1;
    }
    document.getElementById("startInput").addEventListener("change", e => {
        start = Math.max(0, parseInt(e.target.value) || 0);
        end = Math.min(total - 1, start + pageSize - 1);
        updateInputs();
        updateArtists();
    });
    document.getElementById("endInput").addEventListener("change", e => {
        end = Math.min(total - 1, parseInt(e.target.value) || end);
        updateInputs();
        updateArtists();
    });

    /* -------------------------
            Search
        ------------------------- */

    const searchInput = document.getElementById("searchcontext");
    searchInput.addEventListener("input", e => {
        const query = e.target.value.trim();
        loadSearch(query);
    });

    /* -------------------------
            Artist Form
        ------------------------- */
    document.getElementById("artistForm").addEventListener("submit", async function (e) {
        e.preventDefault();

        const form = this;
        const title = form.querySelector(".modal-title").textContent.trim();
        const formData = new FormData(form);

        // 🌸 choose route depending on title
        let url = "";
        if (title === "Add Artist") {
            url = "/Artist/Create";
        } else if (title === "Edit Artist") {
            url = "/Artist/Edit";
        } else {
            alert("Unknown action — please reopen the modal.");
            return;
        }
        try {
            const response = await fetch(url, {
                method: "POST",
                body: formData
            });

            if (response.ok) {
                bootstrap.Modal.getInstance(document.getElementById("artistModal")).hide();
                showToast("Artist saved successfully!", "success");
                setTimeout(() => loadArtists(), 500); // refresh artist list
            } else {
                const error = await response.text();
                alert("Error: " + error);
            }
        } catch (err) {
            console.error(err);
            alert("An unexpected error occurred, my love.");
        }
    });
    artistModal.addEventListener('hide.bs.modal', () => {
        document.activeElement.blur();
    });
    /* -------------------------
            Album Form
        ------------------------- */
    document.getElementById("albumForm").addEventListener("submit", async function (e) {
        e.preventDefault();

        const form = this;
        const title = form.querySelector(".modal-title").textContent.trim();
        const formData = new FormData(form);
        const artistId = formData.get("artistId"); // 🌸 get hidden artistId

        // 🌸 choose route depending on title
        let url = "";
        if (title === "Add Album") {
            url = "/Album/Create";
        } else if (title === "Edit Album") {
            url = "/Album/Edit";
        } else {
            alert("Unknown action — please reopen the modal.");
            return;
        }
        try {
            const response = await fetch(url, {
                method: "POST",
                body: formData
            });

            if (response.ok) {
                bootstrap.Modal.getInstance(document.getElementById("albumModal")).hide();
                showToast("Album saved successfully!", "success");
                loadAlbums(artistId); // 💖 refresh albums for this artist
            } else {
                const error = await response.text();
                alert("Error: " + error);
            }
        } catch (err) {
            console.error(err);
            alert("An unexpected error occurred, my love.");
        }
    });
    albumModal.addEventListener('hide.bs.modal', () => {
        document.activeElement.blur();
    });

    /* -------------------------
            Delete Form
        ------------------------- */
    document.getElementById("deleteForm").addEventListener("submit", async function (e) {
        e.preventDefault();

        const form = this;
        const title = form.querySelector(".modal-title").textContent.trim();
        const formData = new FormData(form);
        const uid = formData.get("id"); // universal id
        const ref = formData.get("type"); // universal id
        const artistId = formData.get("artistId"); // universal id
        const albumId = formData.get("albumId"); // universal id

        // 🌸 choose route depending on title
        // 1 means artist delete, 2 means album delete, 3 means song delete
        let url = "";
        if (ref == 1) {
            url = "/Artist/Delete";
        } else if (ref == 2) {
            url = "/Album/Delete";
        } else if (ref == 3) {
            url = "/Song/Delete";
        } else {
            alert("Unknown action — please reopen the modal.");
            return;
        }
        try {
            const response = await fetch(url, {
                method: "POST",
                body: formData
            });

            if (response.ok) {
                bootstrap.Modal.getInstance(document.getElementById("confirmDeleteModal")).hide();
                if (ref == 1) {
                    showToast("Artist deleted successfully!", "success");
                    setTimeout(() => loadArtists(), 500); // refresh artist list
                } else if (ref == 2) {
                    showToast("Album deleted successfully!", "success");
                    setTimeout(() => loadAlbums(artistId)); // refresh album list
                } else if (ref == 3) {
                    showToast("Song deleted successfully!", "success");
                    setTimeout(() => loadSongs(albumId)); // refresh album list
                } else {
                    alert("Unknown action — please refresh the page.");
                    return;
                }
            } else {
                const error = await response.text();
                alert("Error: " + error);
            }
        } catch (err) {
            console.error(err);
            alert("An unexpected error occurred, my love.");
        }
    });
    deleteModal.addEventListener('hide.bs.modal', () => {
        if (document.activeElement) document.activeElement.blur();
    });

    /* -------------------------
            Song Form
        ------------------------- */
    document.getElementById("songForm").addEventListener("submit", async function (e) {
        e.preventDefault();

        const form = this;
        const title = form.querySelector(".modal-title").textContent.trim();
        const formData = new FormData(form);
        const albumId = formData.get("albumId"); // 🌸 get hidden albumId

        // 🌸 choose route depending on title
        let url = "";
        if (title === "Add Song") {
            url = "/Song/Upload";
        } else if (title === "Edit Song") {
            url = "/Song/Edit";
        } else {
            alert("Unknown action — please reopen the modal.");
            return;
        }
        try {
            const response = await fetch(url, {
                method: "POST",
                body: formData
            });

            if (response.ok) {
                bootstrap.Modal.getInstance(document.getElementById("songModal")).hide();
                showToast("Song saved successfully!", "success");
                loadSongs(albumId);
            } else {
                const error = await response.text();
                alert("Error: " + error);
            }
        } catch (err) {
            console.error(err);
            alert("An unexpected error occurred, my love.");
        }
    });
    songModal.addEventListener('hide.bs.modal', () => {
        document.activeElement.blur();
    });
});