import React from 'react'
import { Link } from 'react-router-dom'

import Script from 'dangerous-html/react'
import PropTypes from 'prop-types'

import './navbar.css'

const Navbar = (props) => {
  return (
    <header className="navbar-navbar">
      <div className="navbar-desktop">
        <div className="navbar-main">
          <div className="navbar-branding">
            <Link to="/" className="navbar-navlink1">
              <img
                alt={props.brandingAlt}
                src={props.brandingSrc}
                className="navbar-finbest"
              />
            </Link>
          </div>
          <div className="navbar-links1"></div>
        </div>
        <div className="navbar-quick-actions">
          <Link to="/" className="navbar-navlink2">
            <div className="navbar-sign-up-btn">
              <span className="navbar-sign-up">Download</span>
            </div>
          </Link>
          <img
            id="open-mobile-menu"
            alt={props.pastedImageAlt}
            src={props.pastedImageSrc}
            className="navbar-hamburger-menu"
          />
        </div>
      </div>
      <div id="mobile-menu" className="navbar-mobile">
        <div className="navbar-top">
          <img
            alt={props.imageAlt}
            src={props.imageSrc}
            className="navbar-image"
          />
          <svg
            id="close-mobile-menu"
            viewBox="0 0 1024 1024"
            className="navbar-icon1"
          >
            <path d="M810 274l-238 238 238 238-60 60-238-238-238 238-60-60 238-238-238-238 60-60 238 238 238-238z"></path>
          </svg>
        </div>
        <div className="navbar-links2">
          <Link to="/" className="navbar-navlink3">
            {props.text1}
          </Link>
          <Link to="/" className="navbar-navlink4">
            {props.text11}
          </Link>
          <Link to="/" className="navbar-navlink5">
            {props.text12}
          </Link>
          <Link to="/" className="navbar-navlink6">
            {props.text13}
          </Link>
          <div className="navbar-buttons">
            <Link to="/" className="navbar-navlink7">
              <div className="navbar-btn1">
                <span className="navbar-text1">{props.text131}</span>
              </div>
            </Link>
            <Link to="/" className="navbar-navlink8">
              <div className="navbar-btn2">
                <span className="navbar-text2">{props.text1311}</span>
              </div>
            </Link>
          </div>
        </div>
      </div>
      <div>
        <div className="navbar-container2">
          <Script
            html={` <script defer>
    /*
    Mobile menu - Code Embed
    */

    // Mobile menu
    const mobileMenu = document.querySelector("#mobile-menu");

    // Buttons
    const closeButton = document.querySelector("#close-mobile-menu");
    const openButton = document.querySelector("#open-mobile-menu");

    if (mobileMenu && closeButton && openButton) {
      // On openButton click, set the mobileMenu position left to -100vw
      openButton.addEventListener("click", function () {
        mobileMenu.style.transform = "translateX(0%)";
      });

      // On closeButton click, set the mobileMenu position to 0vw
      closeButton.addEventListener("click", function () {
        mobileMenu.style.transform = "translateX(100%)";
      });
    }
  </script>`}
          ></Script>
        </div>
      </div>
    </header>
  )
}

Navbar.defaultProps = {
  text1: 'Features',
  text12: 'Prices',
  brandingSrc: '/pastedimage-cx4wqj.svg',
  text131: 'Log in',
  imageAlt: 'image',
  pastedImageSrc: '/pastedimage-8o98.svg',
  text1311: 'Sign up',
  text13: 'Contact',
  pastedImageAlt: 'pastedImage',
  imageSrc: '/pastedimage-cx4wqj.svg',
  brandingAlt: 'pastedImage',
  text11: 'How it works',
}

Navbar.propTypes = {
  text1: PropTypes.string,
  text12: PropTypes.string,
  brandingSrc: PropTypes.string,
  text131: PropTypes.string,
  imageAlt: PropTypes.string,
  pastedImageSrc: PropTypes.string,
  text1311: PropTypes.string,
  text13: PropTypes.string,
  pastedImageAlt: PropTypes.string,
  imageSrc: PropTypes.string,
  brandingAlt: PropTypes.string,
  text11: PropTypes.string,
}

export default Navbar
