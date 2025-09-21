$(document).ready(function () {
    /* -------------------------
       Album & Artist Modals
    ------------------------- */
    window.openAlbumModal = function (id = 0, title = '', artistId = '', releaseDate = '', coverUrl = '') {
        $("#albumId").val(id);
        $("#albumTitle").val(title);
        $("#albumArtistId").val(artistId);
        $("#albumReleaseDate").val(releaseDate);
        $("#albumCoverUrl").val(coverUrl);
        $("#albumForm").attr("action", id === 0 ? "/Album/Create" : "/Album/Edit");
    };

    window.openArtistModal = function (id = 0, name = '', bio = '') {
        $("#artistId").val(id);
        $("#artistName").val(name);
        $("#artistBio").val(bio);
        $("#artistForm").attr("action", id === 0 ? "/Artist/Create" : "/Artist/Edit");
    };

    /* -------------------------
       Song Modal (Bootstrap)
    ------------------------- */
    const songModal = new bootstrap.Modal(document.getElementById("songModal"));

    window.openSongModal = function (id = '', title = '', albumId = '', artistId = '') {
        $("#songId").val(id);
        $("#songTitle").val(title);
        $("#artistSelect").val(artistId);
        filterAlbums();
        $("#albumSelect").val(albumId);

        $("#songForm").attr("action", id ? "/Song/Edit" : "/Song/Upload");
        $("#modaltitlesong").text(id ? "Edit Song" : "Add Song");

        songModal.show();
    }

    $("#songForm").on("submit", function (e) {
        e.preventDefault();
        const formData = new FormData(this);
        const url = $(this).attr("action");

        $.ajax({
            type: "POST",
            url: url,
            data: formData,
            contentType: false,
            processData: false,
            success: function () {
                songModal.hide();
                location.reload();
            }
        });
    });

    window.deleteSong = function (id) {
        if (!confirm("Are you sure?")) return;
        $.post("/Song/Delete", { id }, function () {
            location.reload();
        });
    }
    window.filterAlbums = function () {
        const artistId = $("#artistSelect").val();
        $("#albumSelect option").each(function () {
            const aId = $(this).data("artist");
            $(this).toggle(!artistId || aId == artistId || $(this).val() === "");
        });
        $("#albumSelect").val("");
    }

    window.filterAlbumsSongs = function () {
        const artistId = $("#searchArtist").val();

        if ($("#searchAlbum option[value='']").length === 0) {
            $("#searchAlbum").prepend('<option value="">All Albums</option>');
        }

        $("#searchAlbum option").each(function () {
            const aId = $(this).data("artist");
            if (!artistId || $(this).val() === "") {
                $(this).show();
            } else {
                $(this).toggle(aId == artistId);
            }
        });

        $("#searchAlbum").val('');
    }

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
       Song Filter
    ------------------------- */
    function filterSongs() {
        const title = $("#searchTitle").val().toLowerCase();
        const artist = $("#searchArtist option:selected").text().toLowerCase();
        const album = $("#searchAlbum option:selected").text().toLowerCase();

        $("#songsTable tbody tr").each(function () {
            const songTitle = $(this).find("td:nth-child(1)").text().toLowerCase();
            const songArtist = $(this).find("td:nth-child(2)").text().toLowerCase();
            const songAlbum = $(this).find("td:nth-child(3)").text().toLowerCase();

            const matchTitle = !title || songTitle.includes(title);
            const matchArtist = artist === "all artists" || songArtist.includes(artist);
            const matchAlbum = album === "all albums" || songAlbum.includes(album);

            $(this).toggle(matchTitle && matchArtist && matchAlbum);
        });
    }

    $(".search-box").on("keyup change", filterSongs);

    /* -------------------------
       Songs Table Pagination
    ------------------------- */
    const rowsPerPage = 20;
    const $rows = $("#songsTable tbody tr");
    const rowsCount = $rows.length;
    const pageCount = Math.ceil(rowsCount / rowsPerPage);

    if (pageCount > 1) {
        let paginationHtml = '<nav><ul class="pagination justify-content-center" id="songsPagination">';
        for (let i = 1; i <= pageCount; i++) {
            paginationHtml += `<li class="page-item"><a class="page-link" href="#" data-page="${i}">${i}</a></li>`;
        }
        paginationHtml += '</ul></nav>';
        $("#songsTable").after(paginationHtml);
    }

    function showSongPage(page) {
        const start = (page - 1) * rowsPerPage;
        const end = start + rowsPerPage;
        $rows.hide().slice(start, end).show();

        $("#songsPagination li").removeClass("active");
        $(`#songsPagination a[data-page='${page}']`).parent().addClass("active");
    }

    $(document).on("click", "#songsPagination a", function (e) {
        e.preventDefault();
        const page = parseInt($(this).data("page"));
        showSongPage(page);
    });


    showSongPage(1);
});

/*
  Robust Artist Pagination + Search
  - Place this AFTER the #artistContainer HTML and after jQuery.
  - Adjust itemsPerPage as needed.
*/
$(function () {
    const $allCards = $("#artistContainer .artist-card");    // all artist card elements
    const $pagination = $("#artistPagination");              // <ul id="artistPagination">
    const $paginationNav = $pagination.closest("nav");       // nav wrapper (so we can hide it)
    const itemsPerPage = 9;                                 // change if you want more/less per page

    let filteredCards = $allCards;  // jQuery collection with the currently filtered cards
    let currentPage = 1;

    function renderPagination() {
        $pagination.empty();

        const totalItems = filteredCards.length;
        const totalPages = Math.ceil(totalItems / itemsPerPage);

        // hide pagination when 0 or 1 page
        if (totalPages <= 1) {
            $paginationNav.hide();
            return;
        }

        $paginationNav.show();
        for (let i = 1; i <= totalPages; i++) {
            const activeClass = (i === currentPage) ? " active" : "";
            $pagination.append(
                `<li class="page-item${activeClass}"><a class="page-link" href="#" data-page="${i}">${i}</a></li>`
            );
        }
    }

    function showPage(page) {
        // clamp page to valid range
        const totalItems = filteredCards.length;
        const totalPages = Math.max(1, Math.ceil(totalItems / itemsPerPage));
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        currentPage = page;

        // hide ALL cards, then show only the ones in the filtered slice
        $allCards.hide();
        const $toShow = filteredCards.slice((page - 1) * itemsPerPage, page * itemsPerPage);
        $toShow.show();

        // update active class on pagination without rebuilding it
        $pagination.find(".page-item").removeClass("active");
        $pagination.find(`.page-link[data-page="${page}"]`).parent().addClass("active");
    }

    function applyFilter() {
        const q = $("#artistSearch").val().toLowerCase().trim();

        if (!q) {
            // no filter -> all cards
            filteredCards = $allCards;
        } else {
            filteredCards = $allCards.filter(function () {
                const name = $(this).find(".card-title").text().toLowerCase();
                const bio = $(this).find(".card-text").text().toLowerCase();
                return name.indexOf(q) !== -1 || bio.indexOf(q) !== -1;
            });
        }

        // always go back to page 1 after filter change
        currentPage = 1;
        renderPagination();
        showPage(1);
    }

    // handle page clicks (delegated)
    $(document).on("click", "#artistPagination .page-link", function (e) {
        e.preventDefault();
        const page = parseInt($(this).data("page"), 10) || 1;
        showPage(page);
    });

    // handle search input
    $("#artistSearch").on("input", function () {
        applyFilter();
    });

    // initialize
    applyFilter();
});


$(document).ready(function () {
    const rowsPerPage = 10; // adjust pagination limit
    const $rows = $("#albumsTable tbody tr");

    function renderPagination(filteredRows) {
        $("#albumsPagination").remove();

        const rowsCount = filteredRows.length;
        const pageCount = Math.ceil(rowsCount / rowsPerPage);

        if (pageCount <= 1) return; // no need for pagination

        let paginationHtml = '<nav id="albumsPagination"><ul class="pagination justify-content-center">';
        for (let i = 1; i <= pageCount; i++) {
            paginationHtml += `<li class="page-item"><a class="page-link" href="#" data-page="${i}">${i}</a></li>`;
        }
        paginationHtml += '</ul></nav>';

        $("#albumsTable").after(paginationHtml);

        // Default show page 1
        showPage(1, filteredRows);
    }

    function showPage(page, filteredRows) {
        const start = (page - 1) * rowsPerPage;
        const end = start + rowsPerPage;

        $rows.hide();
        filteredRows.slice(start, end).show();

        $("#albumsPagination li").removeClass("active");
        $(`#albumsPagination a[data-page='${page}']`).parent().addClass("active");
    }

    function applySearchAndPagination() {
        const query = $("#albumSearch").val().toLowerCase();
        const filteredRows = $rows.filter(function () {
            return $(this).text().toLowerCase().indexOf(query) > -1;
        });

        $rows.hide();
        filteredRows.show();

        renderPagination(filteredRows);
    }

    // Event: search
    $("#albumSearch").on("keyup", function () {
        applySearchAndPagination();
    });

    // Event: pagination click
    $(document).on("click", "#albumsPagination a", function (e) {
        e.preventDefault();
        const page = parseInt($(this).data("page"));
        const query = $("#albumSearch").val().toLowerCase();
        const filteredRows = $rows.filter(function () {
            return $(this).text().toLowerCase().indexOf(query) > -1;
        });
        showPage(page, filteredRows);
    });

    // Initialize first load
    applySearchAndPagination();
});