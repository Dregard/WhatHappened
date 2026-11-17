if(NOT TARGET games-frame-pacing::swappy)
add_library(games-frame-pacing::swappy SHARED IMPORTED)
set_target_properties(games-frame-pacing::swappy PROPERTIES
    IMPORTED_LOCATION "C:/Users/karda/.gradle/caches/8.13/transforms/4ecb73e96403463e3d40eb44fb7a054f/transformed/jetified-games-frame-pacing-2.1.2/prefab/modules/swappy/libs/android.arm64-v8a/libswappy.so"
    INTERFACE_INCLUDE_DIRECTORIES "C:/Users/karda/.gradle/caches/8.13/transforms/4ecb73e96403463e3d40eb44fb7a054f/transformed/jetified-games-frame-pacing-2.1.2/prefab/modules/swappy/include"
    INTERFACE_LINK_LIBRARIES ""
)
endif()

if(NOT TARGET games-frame-pacing::swappy_static)
add_library(games-frame-pacing::swappy_static STATIC IMPORTED)
set_target_properties(games-frame-pacing::swappy_static PROPERTIES
    IMPORTED_LOCATION "C:/Users/karda/.gradle/caches/8.13/transforms/4ecb73e96403463e3d40eb44fb7a054f/transformed/jetified-games-frame-pacing-2.1.2/prefab/modules/swappy_static/libs/android.arm64-v8a/libswappy_static.a"
    INTERFACE_INCLUDE_DIRECTORIES "C:/Users/karda/.gradle/caches/8.13/transforms/4ecb73e96403463e3d40eb44fb7a054f/transformed/jetified-games-frame-pacing-2.1.2/prefab/modules/swappy_static/include"
    INTERFACE_LINK_LIBRARIES ""
)
endif()

